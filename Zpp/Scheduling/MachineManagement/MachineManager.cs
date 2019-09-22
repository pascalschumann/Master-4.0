using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Microsoft.EntityFrameworkCore.Internal;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.OrderGraph;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.Mrp.MachineManagement
{
    public class MachineManager : IMachineManager
    {
        private readonly IDbMasterDataCache _dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();
        
        public void JobSchedulingWithGifflerThompsonAsZaepfel(
            IDbTransactionData dbTransactionData,
            IPriorityRule priorityRule)
        {
            IProductionOrderToOperationGraph<INode> productionOrderToOperationGraph =
                new ProductionOrderToOperationGraph(dbTransactionData);

            Dictionary<Id, List<Resource>> resourcesByResourceSkillId =
                new Dictionary<Id, List<Resource>>();
            foreach (var resourceSkill in _dbMasterDataCache.M_ResourceSkillGetAll())
            {
                resourcesByResourceSkillId.Add(resourceSkill.GetId(),
                    dbTransactionData.GetAggregator()
                        .GetResourcesByResourceSkillId(resourceSkill.GetId()));
            }

            /*
            S: Menge der aktuell einplanbaren Arbeitsvorgänge
            a: Menge der technologisch an erster Stelle eines Fertigungsauftrags stehenden Arbeitsvorgänge
            N(o): Menge der technologisch direkt nachfolgenden Arbeitsoperationen von Arbeitsoperation o
            M(o): Maschine auf der die Arbeitsoperation o durchgeführt wird
            K: Konfliktmenge (die auf einer bestimmten Maschine gleichzeitig einplanbaren Arbeitsvorgänge)            
            p(o): Bearbeitungszeit von Arbeitsoperation o (=Duration)
            t(o): Startzeit der Operation o (=Start)
            d(o): Fertigstellungszeitpunkt von Arbeitsoperation o (=End)
            d_min: Minimum der Fertigstellungszeitpunkte
            o_min: Operation mit minimalem Fertigstellungszeitpunkt
            o1: beliebige Operation aus K (o_dach bei Zäpfel)
            */
            IStackSet<ProductionOrderOperation> S = new StackSet<ProductionOrderOperation>();

            // Bestimme initiale Menge: S = a
            S = CreateS(productionOrderToOperationGraph);

            // t(o) = 0 für alle o aus S
            foreach (var o in S)
            {
                o.GetValue().Start = o.GetValue().StartBackward.GetValueOrDefault();
            }

            // while S not empty do
            while (S != null && S.Any())
            {
                int d_min = Int32.MaxValue;
                ProductionOrderOperation o_min = null;
                foreach (var o in S)
                {
                    // Berechne d(o) = t(o) + p(o) für alle o aus S
                    o.GetValue().End = o.GetValue().Start + o.GetValue().Duration;
                    // Bestimme d_min = min{ d(o) | o aus S }
                    if (o.GetValue().End < d_min)
                    {
                        d_min = o.GetValue().End;
                        o_min = o;
                    }
                }

                // Bilde Konfliktmenge K = { o | o aus S UND M(o) == M(o_min) UND t(o) < d_min }
                IStackSet<ProductionOrderOperation> K = new StackSet<ProductionOrderOperation>();
                foreach (var o in S)
                {
                    if (o.GetValue().ResourceSkillId.Equals(o_min.GetValue().ResourceSkillId) &&
                        o.GetValue().Start < d_min)
                    {
                        K.Push(o);
                    }
                }

                // while K not empty do
                if (K.Any())
                {
                    // Entnehme Operation mit höchster Prio (o1) aus K und plane auf nächster freier Resource ein

                    List<ProductionOrderOperation> allO1 = new List<ProductionOrderOperation>();

                    foreach (var machine in resourcesByResourceSkillId[o_min.GetResourceSkillId()]
                        .OrderBy(x => x.GetIdleStartTime().GetValue()))
                    {
                        if (K.Any() == false)
                        {
                            break;
                        }

                        ProductionOrderOperation o1 = null;
                        o1 = priorityRule.GetHighestPriorityOperation(machine.GetIdleStartTime(),
                            K.GetAll(), dbTransactionData);
                        if (o1 == null)
                        {
                            throw new MrpRunException("This is not possible if K.Any() is true.");
                        }

                        allO1.Add(o1);

                        K.Remove(o1);

                        o1.SetMachine(machine);
                        // correct op's start time if resource's idleTime is later
                        if (machine.GetIdleStartTime().GetValue() > o1.GetValue().Start)
                        {
                            o1.GetValue().Start = machine.GetIdleStartTime().GetValue();
                            o1.GetValue().End = o1.GetValue().Start + o1.GetValue().Duration;
                        }

                        // correct op's start time if op's material is later available
                        DueTime dueTimeOfOperationMaterial =
                            o1.GetDueTimeOfItsMaterial(dbTransactionData);
                        if (dueTimeOfOperationMaterial.GetValue() > o1.GetValue().Start)
                        {
                            o1.GetValue().Start = dueTimeOfOperationMaterial.GetValue();
                            o1.GetValue().End = o1.GetValue().Start + o1.GetValue().Duration;
                        }

                        machine.SetIdleStartTime(new DueTime(o1.GetValue().End));
                    }


                    // t(o) = d(o1) für alle o aus K ohne alle o1 
                    foreach (var o in K)
                    {
                        o.GetValue().Start = allO1[0].GetValue().End;
                    }

                    /*if N(o1) not empty then
                        S = S vereinigt N(o1) ohne alle o1
                     */
                    foreach (var o1 in allO1)
                    {
                        ProductionOrder productionOrder = o1.GetProductionOrder(dbTransactionData);

                        IStackSet<INode> predecessorOperations = new StackSet<INode>();
                        productionOrderToOperationGraph.GetPredecessorOperations(
                            predecessorOperations, o1);

                        IStackSet<ProductionOrderOperation> N = predecessorOperations
                            .As<ProductionOrderOperation>();

                        // t(o) = d(o1) für alle o aus N(o1)
                        if (N != null)
                        {
                            foreach (var n in N)
                            {
                                /*AdaptPredecessorNodes(N, o1, productionOrderGraph,
                                    productionOrderOperationGraphs);*/
                                // adapt only if o1's end is later than currentOperation else scheduled time from backwards-scheduling will be ignored
                                /*if (o1.GetValue().End > productionOrderOperation.GetValue().Start)
                                {*/
                                n.GetValue().Start = o1.GetValue().End;
                                //}
                            }
                        }

                        // prepare for next round
                        productionOrderToOperationGraph.RemoveNode(o1);
                        /*productionOrderOperationGraph
                            .RemoveProductionOrdersWithNoProductionOrderOperations(
                                productionOrderGraph, productionOrder);*/
                    }

                    S = CreateS(productionOrderToOperationGraph);
                }
            }
        }

        /**
         * @return: all leafs of all operationGraphs
         */
        private IStackSet<ProductionOrderOperation> CreateS(
            IProductionOrderToOperationGraph<INode> productionOrderToOperationGraph)
        {
            IStackSet<INode> S = new StackSet<INode>();
            productionOrderToOperationGraph.GetLeafOperations(S);

            return S.As<ProductionOrderOperation>();
        }
    }
}