using System;
using System.Linq;
using Master40.DB.Data.Context;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.SimulationCore.DistributionProvider;
using Master40.SimulationCore.Environment.Options;
using Priority_Queue;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.WrappersForCollections;
using Zpp.Scheduling;
using Zpp.Scheduling.impl;
using Zpp.Scheduling.impl.JobShop.impl;
using Zpp.Simulation.impl.Types;
using Zpp.Test.Configuration.Scenarios;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.Util.Queue;

namespace Zpp.Mrp.impl
{
    public class Mrp : IMrp
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly JobShopScheduler _jobShopScheduler = new JobShopScheduler();
        private IOrderGenerator _orderGenerator = null;

        public Mrp()
        {
            ProductionDomainContext productionDomainContext =
                ZppConfiguration.CacheManager.GetProductionDomainContext();
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
            // ScheduleForward();

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
            DemandToProviderGraph demandToProviderGraph =
                new DemandToProviderGraph();
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
            DemandToProviderGraph demandToProviderGraph)
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
            CreateOrders(interval, null);
        }

        public void CreateOrders(SimulationInterval interval, Quantity customerOrderQuantity)
        {
            IDbMasterDataCache masterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            OrderArrivalRate orderArrivalRate;
            if (customerOrderQuantity == null)
            {
                orderArrivalRate =
                    new OrderArrivalRate(0.025);
            }
            else
            {
                // (Menge der zu erzeugenden auftrage im intervall +1) / (die dauer des intervalls)
                // works only small numbers e.g. 10
                orderArrivalRate =
                    new OrderArrivalRate((double) (customerOrderQuantity.GetValue() * 2) /
                                         interval.Interval);
            }

            if (_orderGenerator == null ||
                _orderGenerator.GetOrderArrivalRate().Equals(orderArrivalRate) == false)
            {
                _orderGenerator = TestScenario.GetOrderGenerator(new MinDeliveryTime(200),
                    new MaxDeliveryTime(1430), orderArrivalRate, masterDataCache.M_ArticleGetAll(),
                    masterDataCache.M_BusinessPartnerGetAll());
            }

            var creationTime = interval.StartAt;
            var endOrderCreation = interval.EndAt;

            // Generate exact given quantity of customerOrders
            while (creationTime < endOrderCreation &&
                   dbTransactionData.T_CustomerOrderGetAll().Count <
                   customerOrderQuantity.GetValue())
            {
                var order = _orderGenerator.GetNewRandomOrder(time: creationTime);
                foreach (var orderPart in order.CustomerOrderParts)
                {
                    orderPart.CustomerOrder = order;
                    orderPart.CustomerOrderId = order.Id;
                    dbTransactionData.CustomerOrderPartAdd(orderPart);
                }

                dbTransactionData.CustomerOrderAdd(order);

                // TODO : Handle this another way (Why Martin ?)
                creationTime += order.CreationTime;
            }
        }
    }
}