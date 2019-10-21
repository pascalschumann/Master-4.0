using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.Context;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.Mrp2.impl.Mrp1;
using Zpp.Mrp2.impl.Scheduling;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Mrp2.impl.Scheduling.impl.JobShopScheduler;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.Mrp2.impl
{
    public class Mrp2 : IMrp2
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IJobShopScheduler _jobShopScheduler = new JobShopScheduler();
        

        

        public Mrp2()
        {
            ProductionDomainContext productionDomainContext =
                ZppConfiguration.CacheManager.GetProductionDomainContext();
        }

        private void ManufacturingResourcePlanning(IDemands dbDemands)
        {
            if (dbDemands == null || dbDemands.Any() == false)
            {
                return;
            }

            // MaterialRequirementsPlanning
            IMrp1 mrp1 = new Mrp1.impl.Mrp1(dbDemands);
            mrp1.StartMrp1();

            OrderOperationGraph orderOperationGraph = new OrderOperationGraph();
            
            ScheduleBackward(orderOperationGraph.GetRootNodes().ToStack(), orderOperationGraph, true);
            
            ScheduleForward();

            INodes childRootNodes = new Nodes();
            foreach (var rootNode in orderOperationGraph.GetRootNodes().ToStackSet())
            {
                IProviders childProviders = ZppConfiguration.CacheManager.GetAggregator()
                    .GetAllChildProvidersOf((Demand) rootNode.GetEntity());
                if (childProviders.Count() != 1)
                {
                    throw new MrpRunException(
                        "A CustomerOrderPart is only allowed to have exact one provider.");
                }

                childRootNodes.AddAll(childProviders.ToNodes());
            }

            ScheduleBackward(childRootNodes.ToStack(), orderOperationGraph, false);

            // job shop scheduling
            JobShopScheduling();

            Logger.Info("MrpRun done.");
        }

        private void ScheduleBackward(Stack<INode> rootNodes, IDirectedGraph<INode> orderOperationGraph,
            bool clearOldTimes)
        {
            IBackwardsScheduler backwardsScheduler =
                new BackwardScheduler(rootNodes, orderOperationGraph, clearOldTimes);
            backwardsScheduler.ScheduleBackward();
        }

        public void ScheduleForward()
        {
            IForwardScheduler forwardScheduler = new ForwardScheduler();
            forwardScheduler.ScheduleForward();
        }

        public void JobShopScheduling()
        {
            _jobShopScheduler.ScheduleWithGifflerThompsonAsZaepfel(new PriorityRule());
        }

        public void StartMrp2()
        {
            // execute mrp2
            Demands unsatisfiedCustomerOrderParts = ZppConfiguration.CacheManager.GetAggregator()
                .GetPendingCustomerOrderParts();
            ManufacturingResourcePlanning(unsatisfiedCustomerOrderParts);

            
        }
    }
}