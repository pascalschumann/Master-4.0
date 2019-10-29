using System.Collections.Generic;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.ZppSimulator.impl.Confirmation.impl
{
    public static class ConfirmationCreator
    {
        public static void CreateConfirmations(SimulationInterval simulationInterval)
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
                demandOrProvider.SetReadOnly();
            }

            // stockExchanges, purchaseOrderParts, operations(use PrBom instead):
            // set finished when endTime is within interval
            DemandOrProviders demandOrProvidersToSetFinished = new DemandOrProviders();
            demandOrProvidersToSetFinished.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinIntervalOrBefore(simulationInterval,
                    new DemandOrProviders(dbTransactionData.PurchaseOrderPartGetAll())));
            demandOrProvidersToSetFinished.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinIntervalOrBefore(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeDemandsGetAll())));
            demandOrProvidersToSetFinished.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinIntervalOrBefore(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeProvidersGetAll())));
            demandOrProvidersToSetFinished.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinIntervalOrBefore(simulationInterval,
                    new DemandOrProviders(dbTransactionData.ProductionOrderBomGetAll())));
            foreach (var demandOrProvider in demandOrProvidersToSetFinished)
            {
                demandOrProvider.SetFinished();
                demandOrProvider.SetReadOnly();
            }

            // customerOrderParts: set finished if all childs are finished
            DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();
            INodes rootNodes = demandToProviderGraph.GetRootNodes();
            foreach (var rootNode in rootNodes)
            {
                if (rootNode.GetEntity().GetType() == typeof(CustomerOrderPart))
                {
                    CustomerOrderPart customerOrderPart = (CustomerOrderPart) rootNode.GetEntity();
                    customerOrderPart.SetReadOnly();

                    bool allChildsAreFinished = true;
                    foreach (var stockExchangeProvider in aggregator.GetAllChildProvidersOf(customerOrderPart))
                    {
                        if (stockExchangeProvider.IsFinished() == false)
                        {
                            allChildsAreFinished = false;
                            break;
                        }
                    }
                    if (allChildsAreFinished)
                    {
                        customerOrderPart.SetFinished();
                    }
                }
            }
            
            // set operations readonly
            foreach (var operation in dbTransactionData.ProductionOrderOperationGetAll())
            {
                operation.SetReadOnly();
            }
            
            // set productionOrders readonly
            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                productionOrder.SetReadOnly();
            }
            
            
        }
    }
}