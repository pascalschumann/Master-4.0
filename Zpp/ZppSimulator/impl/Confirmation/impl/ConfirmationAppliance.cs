using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.Graph;

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
            IProviders copyOfProductionOrders = new Providers();
            copyOfProductionOrders.AddAll(dbTransactionData.ProductionOrderGetAll());

            foreach (var productionOrder in copyOfProductionOrders)
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

            RemoveAllArrowsOnFinishedPurchaseOrderParts(dbTransactionData, aggregator);

            // Lösche alle children der COPs (StockExchangeProvider) inclusive Pfeile auf und weg
            RemoveChildsOfCustomerOrderPartsIncludingArrows(dbTransactionData, aggregator);
            
            SetReadOnly(dbTransactionData.StockExchangeProvidersGetAll());
            SetReadOnly(dbTransactionData.StockExchangeDemandsGetAll());
            
            ArchiveFinishedCustomerOrderParts(dbTransactionData, dbTransactionDataArchive);
            ArchiveFinishedPurchaseOrderParts(dbTransactionData, dbTransactionDataArchive);
        }

        private static void SetReadOnly(IEnumerable<IDemandOrProvider> demandOrProviders)
        {
            foreach (var demandOrProvider in demandOrProviders)
            {
                demandOrProvider.SetReadOnly();
            }
        }

        /**
         * Subgraph of a productionOrder includes:
         * - parent (StockExchangeDemand)
         * - childs (ProductionOrderBoms)
         * - childs of childs (StockExchangeProvider)
         */
        private static List<IDemandOrProvider> CreateProductionOrderSubGraph(
            bool IncludeParentStockExchangeDemand, ProductionOrder productionOrder,
            IAggregator aggregator)
        {
            List<IDemandOrProvider> demandOrProvidersOfProductionOrderSubGraph =
                new List<IDemandOrProvider>();
            demandOrProvidersOfProductionOrderSubGraph.Add(productionOrder);

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
            /*
             stockExchangeProvider doesn't belong to subgraph anymore
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
            }*/

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
                CreateProductionOrderSubGraph(true, productionOrder, aggregator);

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
        }

        private static void HandleProductionOrderIsFinished(ProductionOrder productionOrder,
            IAggregator aggregator, IDbTransactionData dbTransactionData)
        {
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();

            // archive operations
            List<ProductionOrderOperation> operations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            SetReadOnlyIfFinished(operations);
            dbTransactionDataArchive.ProductionOrderOperationAddAll(operations);
            dbTransactionData.ProductionOrderOperationDeleteAll(operations);

            // collect demands Or providers
            List<IDemandOrProvider> demandOrProvidersToArchive =
                CreateProductionOrderSubGraph(false, productionOrder, aggregator);
            // set readOnly
            SetReadOnlyIfFinished(demandOrProvidersToArchive);

            // delete archive all collected entities
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
                    SetReadOnlyIfFinished(purchaseOrderPart);
                    
                    // archive it
                    Id purchaseOrderId = new Id(((PurchaseOrderPart) purchaseOrderPart).GetValue()
                        .PurchaseOrderId);
                    purchaseOrderIds.Add(purchaseOrderId);
                    dbTransactionDataArchive.ProvidersAdd(purchaseOrderPart);
                    dbTransactionData.ProvidersDelete(purchaseOrderPart);
                }
            }

            foreach (var purchaseOrderId in purchaseOrderIds)
            {
                T_PurchaseOrder purchaseOrder =
                    dbTransactionData.PurchaseOrderGetById(purchaseOrderId);
                dbTransactionDataArchive.PurchaseOrderAdd(purchaseOrder);
                dbTransactionData.PurchaseOrderDelete(purchaseOrder);
            }
        }

        private static void SetReadOnlyIfFinished(IScheduleNode scheduleNode)
        {
            if (scheduleNode.IsFinished())
            {
                scheduleNode.SetReadOnly();
            }
        }
        
        private static void SetReadOnlyIfFinished(IEnumerable<IScheduleNode> scheduleNodes)
        {
            foreach (var scheduleNode in scheduleNodes)
            {
                SetReadOnlyIfFinished(scheduleNode);
            }
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
                    SetReadOnlyIfFinished(customerOrderPart);
                    
                    // archive it
                    Id customerOrderId = new Id(((CustomerOrderPart) customerOrderPart).GetValue()
                        .CustomerOrderId);
                    idsOfCustomerOrders.Add(customerOrderId);
                    dbTransactionDataArchive.DemandsAdd(customerOrderPart);
                    dbTransactionData.DemandsDelete(customerOrderPart);
                }
            }

            foreach (var idsOfCustomerOrder in idsOfCustomerOrders)
            {
                T_CustomerOrder customerOrder =
                    dbTransactionData.CustomerOrderGetById(idsOfCustomerOrder);
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