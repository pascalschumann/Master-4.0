using System.Collections.Generic;
using System.Linq;
using Master40.DB.DataModel;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.ZppSimulator.impl.Confirmation.impl
{
    public class ConfirmationManager : IConfirmationManager
    {
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
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            // Lösche alle children der COPs (StockExchangeProvider) inclusive Pfeile auf und weg
            foreach (var customerOrderPart in dbTransactionData.T_CustomerOrderPartGetAll())
            {
                IProviders providers = aggregator.GetAllChildProvidersOf(customerOrderPart);
                if (providers.Count() > 1)
                {
                    throw new MrpRunException("A customerOrderPart can only have one provider.");
                }

                foreach (var provider in providers)
                {
                    IEnumerable<T_DemandToProvider> demandToProviders = dbTransactionData
                        .DemandToProviderGetAll().GetAll()
                        .Where(x => x.GetProviderId().Equals(provider.GetId()));
                    IEnumerable<T_ProviderToDemand> providerToDemands = dbTransactionData
                        .ProviderToDemandGetAll().GetAll()
                        .Where(x => x.GetProviderId().Equals(provider.GetId()));
                    dbTransactionData.DeleteAllDemandToProvider(demandToProviders);
                    dbTransactionData.DeleteAllProviderToDemand(providerToDemands);
                    dbTransactionData.DeleteStockExchangeProvider((StockExchangeProvider) provider);
                }
            }

            // ProductionOrder: 3 Zustände siehe DA
            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                ProductionOrderState state =
                    DetermineProductionOrderState((ProductionOrder) productionOrder, aggregator);
                switch (state)
                {
                    case ProductionOrderState.Created:
                        ApplyProductionOrderIsInStateCreated(); break;
                    case ProductionOrderState.InProgress:
                        ApplyProductionOrderIsInProgress(); break;
                    case ProductionOrderState.Done:
                        ApplyProductionOrderIsDone(); break;
                    default:  throw new MrpRunException("This state is not expected.");
                }
            }
        }

        private void ApplyProductionOrderIsInStateCreated()
        {
            // TODO
        }
        
        private void ApplyProductionOrderIsInProgress()
        {
            // TODO
        }
        
        private void ApplyProductionOrderIsDone()
        {
            // TODO
        }

        private ProductionOrderState DetermineProductionOrderState(ProductionOrder productionOrder,
            IAggregator aggregator)
        {
            bool atLeastOneIsInProgress = false;
            bool atLeastOneIsDone = false;
            bool atLeastOneIsInStateCreated = false;
            foreach (var productionOrderOperation in aggregator
                .GetProductionOrderOperationsOfProductionOrder(productionOrder))
            {
                if (productionOrderOperation.IsInProgress())
                {
                    atLeastOneIsInProgress = true;
                    break;
                }
                else if (productionOrderOperation.IsDone())
                {
                    atLeastOneIsDone = true;
                }
                else
                {
                    atLeastOneIsInStateCreated = true;
                }
            }

            if (atLeastOneIsInProgress || atLeastOneIsInStateCreated && atLeastOneIsDone)
            {
                return ProductionOrderState.InProgress;
            }
            else if (atLeastOneIsInStateCreated && !atLeastOneIsInProgress && !atLeastOneIsDone)
            {
                return ProductionOrderState.Created;
            }
            else if (atLeastOneIsDone && !atLeastOneIsInProgress && !atLeastOneIsInStateCreated)
            {
                return ProductionOrderState.Created;
            }
            else
            {
                throw new MrpRunException("This state is not expected.");
            }
        }
    }
}