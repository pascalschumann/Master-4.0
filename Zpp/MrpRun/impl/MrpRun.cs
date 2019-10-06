using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Master40.DB.Data.Context;
using Master40.DB.Data.Helper;
using Master40.DB.DataModel;
using Master40.SimulationCore.DistributionProvider;
using Master40.SimulationCore.Environment.Options;
using Priority_Queue;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.NodeManagement;
using Zpp.Mrp.Scheduling;
using Zpp.Mrp.StockManagement;
using Zpp.OrderGraph;
using Zpp.Simulation.Types;
using Zpp.Test.Configuration.Scenarios;
using Zpp.Utils;
using Zpp.Utils.Queue;
using Zpp.WrappersForCollections;

namespace Zpp.Mrp
{
    public class MrpRun : IMrpRun
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly JobShopScheduler _jobShopScheduler = new JobShopScheduler();
        private readonly OrderGenerator _orderGenerator;

        public MrpRun()
        {
            ProductionDomainContext productionDomainContext =
                ZppConfiguration.CacheManager.GetProductionDomainContext();

            _orderGenerator = TestScenario.GetOrderGenerator(productionDomainContext,
                new MinDeliveryTime(200), new MaxDeliveryTime(1430),
                new OrderArrivalRate(0.025));
        }

        public EntityCollector MaterialRequirementsPlanning(Demand demand,
            IProviderManager providerManager)
        {
            EntityCollector entityCollector = new EntityCollector();

            EntityCollector response = providerManager.Satisfy(demand, demand.GetQuantity());
            entityCollector.AddAll(response);
            providerManager.AdaptStock(response.GetProviders());
            response = providerManager.CreateDependingDemands(entityCollector.GetProviders());
            entityCollector.AddAll(response);

            if (entityCollector.IsSatisfied(demand) == false)
            {
                throw new MrpRunException($"'{demand}' was NOT satisfied: remaining is " +
                                          $"{entityCollector.GetRemainingQuantity(demand)}");
            }

            return entityCollector;
        }


        public void ManufacturingResourcePlanning(IDemands dbDemands)
        {
            if (dbDemands == null || dbDemands.Any() == false)
            {
                return;
            }

            // init
            int MAX_DEMANDS_IN_QUEUE = 100000;

            FastPriorityQueue<DemandQueueNode> demandQueue =
                new FastPriorityQueue<DemandQueueNode>(MAX_DEMANDS_IN_QUEUE);

            IProviderManager providerManager = new ProviderManager();

            foreach (var demand in dbDemands)
            {
                demandQueue.Enqueue(new DemandQueueNode(demand), demand.GetDueTime().GetValue());
            }

            // MaterialRequirementsPlanning
            EntityCollector allCreatedEntities = new EntityCollector();
            while (demandQueue.Count != 0)
            {
                DemandQueueNode firstDemandInQueue = demandQueue.Dequeue();

                EntityCollector response =
                    MaterialRequirementsPlanning(firstDemandInQueue.GetDemand(), providerManager);
                allCreatedEntities.AddAll(response);

                // TODO: EnqueueAll()
                foreach (var demand in response.GetDemands())
                {
                    demandQueue.Enqueue(new DemandQueueNode(demand),
                        demand.GetDueTime().GetValue());
                }
            }

            // write data to _dbTransactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            dbTransactionData.AddAll(allCreatedEntities);
            // End of MaterialRequirementsPlanning

            // forward scheduling
            ScheduleForward();

            // job shop scheduling
            JobShopScheduling();

            Logger.Info("MrpRun done.");
        }

        public void ScheduleBackward()
        {
            throw new NotImplementedException();
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

        public void CreateConfirmations(SimulationInterval simulationInterval)
        {
            /*ISimulator simulator = new Simulator();
            simulator.ProcessCurrentInterval(simulationInterval, _orderGenerator);*/
            // --> does not work correctly, use trivial impl instead

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            // stockExchanges, purchaseOrderParts, operations(use PrBom instead):
            // set in progress when startTime is within interval
            DemandOrProviders demandOrProvidersToSetInProgress = new DemandOrProviders();
            demandOrProvidersToSetInProgress.AddAll(
                aggregator.GetDemandsOrProvidersWhereStartTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.PurchaseOrderPartGetAll())));
            demandOrProvidersToSetInProgress.AddAll(
                aggregator.GetDemandsOrProvidersWhereStartTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeDemandsGetAll())));
            demandOrProvidersToSetInProgress.AddAll(
                aggregator.GetDemandsOrProvidersWhereStartTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeProvidersGetAll())));
            demandOrProvidersToSetInProgress.AddAll(
                aggregator.GetDemandsOrProvidersWhereStartTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.ProductionOrderBomGetAll())));

            foreach (var demandOrProvider in demandOrProvidersToSetInProgress)
            {
                demandOrProvider.SetInProgress();
            }

            // stockExchanges, purchaseOrderParts, operations(use PrBom instead):
            // set done when endTime is within interval
            DemandOrProviders demandOrProvidersToSetDone = new DemandOrProviders();
            demandOrProvidersToSetDone.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.PurchaseOrderPartGetAll())));
            demandOrProvidersToSetDone.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeDemandsGetAll())));
            demandOrProvidersToSetDone.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeProvidersGetAll())));
            demandOrProvidersToSetDone.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.ProductionOrderBomGetAll())));
            foreach (var demandOrProvider in demandOrProvidersToSetDone)
            {
                demandOrProvider.SetDone();
            }
            
            // customerOrderParts: set done if all childs are done
            DemandToProviderDirectedGraph demandToProviderGraph =
                new DemandToProviderDirectedGraph();
            INodes rootNodes = demandToProviderGraph.GetRootNodes();
            foreach (var rootNode in rootNodes)
            {
                bool isDone = processChilds(demandToProviderGraph.GetSuccessorNodes(rootNode),
                    demandToProviderGraph);
                if (isDone)
                {
                    CustomerOrderPart customerOrderPart = (CustomerOrderPart) rootNode.GetEntity();
                    customerOrderPart.SetDone();
                }
            }
        }

        private bool processChilds(INodes childs,
            DemandToProviderDirectedGraph demandToProviderGraph)
        {
            if (childs == null)
            {
                return true;
            }

            foreach (var child in childs)
            {
                IDemandOrProvider demandOrProvider = (IDemandOrProvider) child.GetEntity();
                if (demandOrProvider.IsDone())
                {
                    return processChilds(demandToProviderGraph.GetSuccessorNodes(child),
                        demandToProviderGraph);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public void ApplyConfirmations()
        {
            /**
             * - löschen aller Verbindungen zwischen P(SE:W) und D(SE:I)
             * - PrO: D(SE:I) bis P(SE:W) erhalten wenn eine der Ops angefangen
             */


            // TODO
        }

        public void CreateOrders(SimulationInterval interval)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            var creationTime = interval.StartAt;
            var endOrderCreation = interval.EndAt;

            while (creationTime < endOrderCreation)
            {
                var order = _orderGenerator.GetNewRandomOrder(time: creationTime);
                foreach (var orderPart in order.CustomerOrderParts)
                {
                    orderPart.CustomerOrder = order;
                    orderPart.CustomerOrderId = order.Id;
                    dbTransactionData.CustomerOrderPartAdd(orderPart);
                }

                dbTransactionData.CustomerOrderAdd(order);

                // TODO : Handle this another way
                creationTime += order.CreationTime;
            }
        }
    }
}