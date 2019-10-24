using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.Util;

namespace Zpp.ZppSimulator.impl.Confirmation.impl
{
    public static class ConfirmationAppliance
    {
        public static void ApplyConfirmations()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            // ProductionOrder: 3 Zustände siehe DA
            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                State state =
                    DetermineProductionOrderState((ProductionOrder) productionOrder, aggregator);
                switch (state)
                {
                    case State.Created:
                        HandleProductionOrderIsInStateCreated((ProductionOrder) productionOrder,
                            aggregator, dbTransactionData);
                        break;
                    case State.InProgress:
                        HandleProductionOrderIsInProgress();
                        break;
                    case State.Finished:
                        HandleProductionOrderIsFinished((ProductionOrder) productionOrder,
                            aggregator, dbTransactionData);
                        break;
                    default: throw new MrpRunException("This state is not expected.");
                }
            }

            // Lösche alle children der COPs (StockExchangeProvider) inclusive Pfeile auf und weg
            RemoveChildsOfCustomerOrderPartsIncludingArrows(dbTransactionData, aggregator);

            RemoveAllArrowsOnFinishedPurchaseOrderParts(dbTransactionData, aggregator);

            ArchiveFinishedCustomerOrderParts(dbTransactionData, dbTransactionDataArchive);
            ArchiveFinishedPurchaseOrderParts(dbTransactionData, dbTransactionDataArchive);
        }

        /**
         * Subgraph of a productionOrder includes:
         * - parent (StockExchangeDemand)
         * - childs (ProductionOrderBoms)
         * - childs of childs (StockExchangeProvider)
         */
        private static List<IDemandOrProvider> GetDemandOrProvidersOfProductionOrderSubGraph(
            bool IncludeParentStockExchangeDemand, ProductionOrder productionOrder,
            IAggregator aggregator)
        {
            List<IDemandOrProvider> demandOrProvidersOfProductionOrderSubGraph =
                new List<IDemandOrProvider>();

            if (IncludeParentStockExchangeDemand)
            {
                IDemands stockExchangeDemands = aggregator.GetAllParentDemandsOf(productionOrder);
                if (stockExchangeDemands.Count() > 1)
                {
                    throw new MrpRunException(
                        "A productionOrder can only have one parentDemand (stockExchangeDemand).");
                }

                demandOrProvidersOfProductionOrderSubGraph.AddRange(stockExchangeDemands);
            }

            IDemands productionOrderBoms = aggregator.GetAllChildDemandsOf(productionOrder);
            demandOrProvidersOfProductionOrderSubGraph.AddRange(productionOrderBoms);
            foreach (var productionOrderBom in productionOrderBoms)
            {
                IProviders stockExchangeProvider =
                    aggregator.GetAllChildProvidersOf(productionOrderBom);
                if (stockExchangeProvider.Count() > 1)
                {
                    throw new MrpRunException(
                        "A ProductionOrderBom can only have one childProvider (stockExchangeProvider).");
                }

                demandOrProvidersOfProductionOrderSubGraph.AddRange(stockExchangeProvider);
            }

            return demandOrProvidersOfProductionOrderSubGraph;
        }

        private static void HandleProductionOrderIsInStateCreated(ProductionOrder productionOrder,
            IAggregator aggregator, IDbTransactionData dbTransactionData)
        {
            // delete all operations
            List<ProductionOrderOperation> operations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            dbTransactionData.ProductionOrderOperationDeleteAll(operations);

            // collect entities and demandToProviders/providerToDemands to delete
            List<IDemandOrProvider> demandOrProvidersToDelete =
                GetDemandOrProvidersOfProductionOrderSubGraph(true, productionOrder, aggregator);

            // delete all collected entities
            foreach (var demandOrProvider in demandOrProvidersToDelete)
            {
                List<ILinkDemandAndProvider> demandAndProviders =
                    aggregator.GetArrowsToAndFrom(demandOrProvider);
                dbTransactionData.DeleteAllFrom(demandAndProviders);

                dbTransactionData.DeleteA(demandOrProvider);
            }
        }

        private static void HandleProductionOrderIsInProgress()
        {
            // nothing to do here
            return;
        }

        private static void HandleProductionOrderIsFinished(ProductionOrder productionOrder,
            IAggregator aggregator, IDbTransactionData dbTransactionData)
        {
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();

            // archive operations
            List<ProductionOrderOperation> operations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            dbTransactionDataArchive.ProductionOrderOperationAddAll(operations);
            dbTransactionData.ProductionOrderOperationDeleteAll(operations);

            // archive demands Or providers
            List<IDemandOrProvider> demandOrProvidersToArchive =
                GetDemandOrProvidersOfProductionOrderSubGraph(false, productionOrder, aggregator);


            // delete all collected entities
            foreach (var demandOrProvider in demandOrProvidersToArchive)
            {
                List<ILinkDemandAndProvider> demandAndProviderLinks =
                    aggregator.GetArrowsToAndFrom(demandOrProvider);
                dbTransactionDataArchive.AddAllFrom(demandAndProviderLinks);
                dbTransactionData.DeleteAllFrom(demandAndProviderLinks);

                dbTransactionDataArchive.AddA(demandOrProvider);
                dbTransactionData.DeleteA(demandOrProvider);
            }
        }

        private static State DetermineProductionOrderState(ProductionOrder productionOrder,
            IAggregator aggregator)
        {
            bool atLeastOneIsInProgress = false;
            bool atLeastOneIsFinished = false;
            bool atLeastOneIsInStateCreated = false;
            var productionOrderOperations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                if (productionOrderOperation.IsInProgress())
                {
                    atLeastOneIsInProgress = true;
                    break;
                }
                else if (productionOrderOperation.IsFinished())
                {
                    atLeastOneIsFinished = true;
                }
                else
                {
                    atLeastOneIsInStateCreated = true;
                }
            }

            if (atLeastOneIsInProgress || atLeastOneIsInStateCreated && atLeastOneIsFinished)
            {
                return State.InProgress;
            }
            else if (atLeastOneIsInStateCreated && !atLeastOneIsInProgress && !atLeastOneIsFinished)
            {
                return State.Created;
            }
            else if (atLeastOneIsFinished && !atLeastOneIsInProgress && !atLeastOneIsInStateCreated)
            {
                return State.Finished;
            }
            else
            {
                throw new MrpRunException("This state is not expected.");
            }
        }

        private static void ArchiveFinishedPurchaseOrderParts(IDbTransactionData dbTransactionData,
            IDbTransactionData dbTransactionDataArchive)
        {
            List<Id> purchaseOrderIds = new List<Id>();
            IProviders purchaseOrderPartsCopy = new Providers();
            purchaseOrderPartsCopy.AddAll(dbTransactionData.PurchaseOrderPartGetAll());
            
            foreach (var purchaseOrderPart in purchaseOrderPartsCopy)
            {
                if (purchaseOrderPart.IsFinished())
                {
                    Id purchaseOrderId = new Id(((PurchaseOrderPart) purchaseOrderPart).GetValue()
                        .PurchaseOrderId);
                    purchaseOrderIds.Add(purchaseOrderId);
                    dbTransactionDataArchive.ProvidersAdd(purchaseOrderPart);
                    dbTransactionData.ProvidersDelete(purchaseOrderPart);
                }
            }

            /*foreach (var purchaseOrderId in purchaseOrderIds)
            {
                T_PurchaseOrder customerOrder = dbTransactionData.PurchaseOrderGetById(purchaseOrderId);
                dbTransactionDataArchive.Pur(customerOrder);
                dbTransactionData.T_CustomerOrderDelete(customerOrder);
                ...
            }*/
        }

        private static void ArchiveFinishedCustomerOrderParts(IDbTransactionData dbTransactionData,
            IDbTransactionData dbTransactionDataArchive)
        {
            List<Id> idsOfCustomerOrders = new List<Id>();
            IDemands customerOrderParts = new Demands();
            customerOrderParts.AddAll(dbTransactionData.CustomerOrderPartGetAll());
            
            foreach (var customerOrderPart in customerOrderParts)
            {
                if (customerOrderPart.IsFinished())
                {
                    Id customerOrderId = new Id(((CustomerOrderPart)customerOrderPart).GetValue().CustomerOrderId);
                    idsOfCustomerOrders.Add(customerOrderId);
                    dbTransactionDataArchive.DemandsAdd(customerOrderPart);
                    dbTransactionData.DemandsDelete(customerOrderPart);
                    
                }
            }

            foreach (var idsOfCustomerOrder in idsOfCustomerOrders)
            {
                T_CustomerOrder customerOrder = dbTransactionData.T_CustomerOrderGetById(idsOfCustomerOrder);
                dbTransactionDataArchive.CustomerOrderAdd(customerOrder);
                dbTransactionData.T_CustomerOrderDelete(customerOrder);
            }
        }

        /**
         * inclusive arrows = DemandToProviders/ProviderToDemands
         */
        private static void RemoveChildsOfCustomerOrderPartsIncludingArrows(
            IDbTransactionData dbTransactionData, IAggregator aggregator)
        {
            foreach (var customerOrderPart in dbTransactionData.CustomerOrderPartGetAll())
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
                    dbTransactionData.DemandToProviderDeleteAll(demandToProviders);
                    dbTransactionData.ProviderToDemandDeleteAll(providerToDemands);
                    dbTransactionData.StockExchangeProvidersDelete(
                        (StockExchangeProvider) provider);
                }
            }
        }

        private static void RemoveAllArrowsOnFinishedPurchaseOrderParts(
            IDbTransactionData dbTransactionData, IAggregator aggregator)
        {
            foreach (var purchaseOrderPart in dbTransactionData.PurchaseOrderPartGetAll())
            {
                if (purchaseOrderPart.IsFinished())
                {
                    List<ILinkDemandAndProvider> demandAndProviderLinks =
                        aggregator.GetArrowsToAndFrom(purchaseOrderPart);
                    dbTransactionData.DeleteAllFrom(demandAndProviderLinks);
                }
            }
        }
    }
}