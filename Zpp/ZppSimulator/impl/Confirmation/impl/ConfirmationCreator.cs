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
            }

            // customerOrderParts: set finished if all childs are finished
            DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();
            INodes rootNodes = demandToProviderGraph.GetRootNodes();
            foreach (var rootNode in rootNodes)
            {
                bool isFinished = ProcessChilds(demandToProviderGraph.GetSuccessorNodes(rootNode),
                    demandToProviderGraph);
                if (isFinished && rootNode.GetEntity().GetType() == typeof(CustomerOrderPart))
                {
                    CustomerOrderPart customerOrderPart = (CustomerOrderPart) rootNode.GetEntity();
                    customerOrderPart.SetFinished();
                }
            }
        }

        /**
         * Top-down traversing demandToProviderGraph
         */
        private static bool ProcessChilds(INodes childs,
            DemandToProviderGraph demandToProviderGraph)
        {
            if (childs == null)
            {
                return true;
            }

            foreach (var child in childs)
            {
                IDemandOrProvider demandOrProvider = (IDemandOrProvider) child.GetEntity();
                if (demandOrProvider.IsFinished())
                {
                    return ProcessChilds(demandToProviderGraph.GetSuccessorNodes(child),
                        demandToProviderGraph);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}