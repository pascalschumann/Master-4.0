using Master40.DB.Data.WrappersForPrimitives;
using Master40.SimulationCore.DistributionProvider;
using Master40.SimulationCore.Environment.Options;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Mrp2;
using Zpp.Test.Configuration.Scenarios;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.ZppSimulator.impl
{
    public class ZppSimulator : IZppSimulator
    {
        const int _interval = 1440;
        private IOrderGenerator _orderGenerator = null;

        public void StartOneCycle(SimulationInterval simulationInterval)
        {
            StartOneCycle(simulationInterval, null);
        }

        public void StartOneCycle(SimulationInterval simulationInterval,
            Quantity customerOrderQuantity)
        {
            // init transactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            CreateOrders(simulationInterval, customerOrderQuantity);
            
            IMrp2 mrp2 = new Mrp2.impl.Mrp2();
            mrp2.StartMrp2();
            
            CreateConfirmations(simulationInterval);

            ApplyConfirmations();

            // persisting cached/created data
            dbTransactionData.PersistDbCache();
            
        }

        /**
         * no confirmations are created and applied
         */
        public void StartTestCycle()
        {
            Quantity customerOrderQuantity = new Quantity(ZppConfiguration.CacheManager
                .GetTestConfiguration().CustomerOrderPartQuantity);

            SimulationInterval simulationInterval = new SimulationInterval(0, _interval);
            
            IMrp2 mrp2 = new Mrp2.impl.Mrp2();

            // init transactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            CreateOrders(simulationInterval, customerOrderQuantity);

            // execute mrp2
            Demands unsatisfiedCustomerOrderParts = ZppConfiguration.CacheManager.GetAggregator()
                .GetPendingCustomerOrderParts();
            mrp2.ManufacturingResourcePlanning(unsatisfiedCustomerOrderParts);
            
            // no confirmations
            
            // persisting cached/created data
            dbTransactionData.PersistDbCache();
        }

        public void StartPerformanceStudy()
        {
            const int maxSimulatingTime = 20160;

            for (int i = 0; i * _interval < maxSimulatingTime; i++)
            {
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval, _interval * (i + 1));
                StartOneCycle(simulationInterval);
            }
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
                orderArrivalRate = new OrderArrivalRate(0.025);
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
            DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();
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

        private bool processChilds(INodes childs, DemandToProviderGraph demandToProviderGraph)
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
    }
}