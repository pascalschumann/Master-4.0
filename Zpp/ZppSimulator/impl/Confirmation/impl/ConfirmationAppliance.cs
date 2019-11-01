using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.OpenDemand;
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

            // RemoveAllArrowsOnFinishedPurchaseOrderParts(dbTransactionData, aggregator);

            RemoveAllArrowsAndStockExchangeProviderOnNotFinishedCustomerOrderParts(
                dbTransactionData, aggregator);

            // ArchiveFinishedCustomerOrderParts(dbTransactionData, dbTransactionDataArchive);


            ArchiveSubGraphOfClosedFinishedStockExchangeDemands(dbTransactionData, aggregator);
        }

        /**
         * Closed means, the stockExchangeDemand cannot server another StockExchangeProvider
         */
        private static void ArchiveSubGraphOfClosedFinishedStockExchangeDemands(
            IDbTransactionData dbTransactionData, IAggregator aggregator)
        {
            List<Demand> copyOfStockExchangeDemands = new List<Demand>();
            copyOfStockExchangeDemands.AddRange(dbTransactionData.StockExchangeDemandsGetAll());

            foreach (var demand in copyOfStockExchangeDemands)
            {
                StockExchangeDemand stockExchangeDemand = (StockExchangeDemand) demand;
                if (OpenDemandManager.IsOpen(stockExchangeDemand) == false &&
                    stockExchangeDemand.IsFinished())
                {
                    ArchiveSubGraphOfStockExchangeDemand(stockExchangeDemand, dbTransactionData,
                        aggregator);
                }
            }
        }

        /**
         * covers parent StockExchangeProvider(and its parent CustomerOrderPart if exist), child PurchaseOrderPart if exist
         * --> 3 types of subgraphs: Production, Purchase, Customer
         */
        private static List<IDemandOrProvider> GetItemsOfStockExchangeDemandSubGraph(
            StockExchangeDemand stockExchangeDemand, IDbTransactionData dbTransactionData,
            IAggregator aggregator, bool includeStockExchangeProviderHavingMultipleChilds)
        {
            List<IDemandOrProvider> items = new List<IDemandOrProvider>();
            items.Add(stockExchangeDemand);

            Providers stockExchangeProviders =
                aggregator.GetAllParentProvidersOf(stockExchangeDemand);
            foreach (var stockExchangeProvider in stockExchangeProviders)
            {
                Demands childsOfStockExchangeProvider =
                    aggregator.GetAllChildDemandsOf(stockExchangeProvider);
                if (includeStockExchangeProviderHavingMultipleChilds ||
                    childsOfStockExchangeProvider.Count() == 1)
                {
                    items.Add(stockExchangeProvider);
                    Demands customerOrderParts =
                        aggregator.GetAllParentDemandsOf(stockExchangeProvider);
                    if (customerOrderParts.Count() > 1)
                    {
                        throw new MrpRunException(
                            "A stockExchangeProvider can only have one parent.");
                    }

                    foreach (var customerOrderPart in customerOrderParts)
                    {
                        items.Add(customerOrderPart);
                    }
                }
            }

            Providers purchaseOrderParts = aggregator.GetAllChildProvidersOf(stockExchangeDemand);
            if (purchaseOrderParts.Count() > 1)
            {
                throw new MrpRunException("A stockExchangeDemand can only have one child.");
            }

            foreach (var purchaseOrderPart in purchaseOrderParts)
            {
                items.Add(purchaseOrderPart);
            }

            return items;
        }


        private static void ArchiveSubGraphOfStockExchangeDemand(
            StockExchangeDemand stockExchangeDemand, IDbTransactionData dbTransactionData,
            IAggregator aggregator)
        {
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();
            List<IDemandOrProvider> demandOrProviders =
                GetItemsOfStockExchangeDemandSubGraph(stockExchangeDemand, dbTransactionData,
                    aggregator, false);

            foreach (var demandOrProvider in demandOrProviders)
            {
                if (demandOrProvider.GetType() == typeof(PurchaseOrderPart))
                {
                    ArchivePurchaseOrderParts(dbTransactionData, dbTransactionDataArchive,
                        (PurchaseOrderPart) demandOrProvider);
                    ArchiveArrowsToAndFrom(demandOrProvider, dbTransactionData,
                        dbTransactionDataArchive, aggregator);
                }
                else if (demandOrProvider.GetType() == typeof(CustomerOrderPart))
                {
                    ArchiveCustomerOrderPart(dbTransactionData, dbTransactionDataArchive,
                        (CustomerOrderPart) demandOrProvider);
                    ArchiveArrowsToAndFrom(demandOrProvider, dbTransactionData,
                        dbTransactionDataArchive, aggregator);
                }
                else
                {
                    ArchiveDemandOrProvider(demandOrProvider, dbTransactionData, aggregator, true);
                }
            }
        }

        private static void ArchiveArrowsToAndFrom(IDemandOrProvider demandOrProvider,
            IDbTransactionData dbTransactionData, IDbTransactionData dbTransactionDataArchive,
            IAggregator aggregator)
        {
            List<ILinkDemandAndProvider> demandAndProviderLinks =
                aggregator.GetArrowsToAndFrom(demandOrProvider);
            foreach (var demandAndProviderLink in demandAndProviderLinks)
            {
                dbTransactionDataArchive.AddA(demandAndProviderLink);
                dbTransactionData.DeleteA(demandAndProviderLink);
            }
        }

        private static void ArchiveDemandOrProvider(IDemandOrProvider demandOrProvider,
            IDbTransactionData dbTransactionData, IAggregator aggregator, bool includeArrows)
        {
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();

            if (includeArrows)
            {
                ArchiveArrowsToAndFrom(demandOrProvider, dbTransactionData,
                    dbTransactionDataArchive, aggregator);
            }

            dbTransactionDataArchive.AddA(demandOrProvider);
            dbTransactionData.DeleteA(demandOrProvider);
        }


        /**
         * Subgraph of a productionOrder includes:
         * - parent (StockExchangeDemand)
         * - childs (ProductionOrderBoms)
         * - childs of childs (StockExchangeProvider)
         */
        private static List<IDemandOrProvider> CreateProductionOrderSubGraph(
            bool includeStockExchanges, ProductionOrder productionOrder, IAggregator aggregator)
        {
            List<IDemandOrProvider> demandOrProvidersOfProductionOrderSubGraph =
                new List<IDemandOrProvider>();
            demandOrProvidersOfProductionOrderSubGraph.Add(productionOrder);

            if (includeStockExchanges)
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

            if (includeStockExchanges)
            {
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
            ArchiveOperations(dbTransactionData, dbTransactionDataArchive, aggregator, productionOrder);

            // collect demands Or providers
            List<IDemandOrProvider> demandOrProvidersToArchive =
                CreateProductionOrderSubGraph(false, productionOrder, aggregator);

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

        private static void ArchivePurchaseOrderParts(IDbTransactionData dbTransactionData,
            IDbTransactionData dbTransactionDataArchive, Provider purchaseOrderPart)
        {
            // archive purchaseOrderPart
            Id purchaseOrderId = new Id(((PurchaseOrderPart) purchaseOrderPart).GetValue()
                .PurchaseOrderId);
            dbTransactionDataArchive.ProvidersAdd(purchaseOrderPart);
            dbTransactionData.ProvidersDelete(purchaseOrderPart);

            // archive purchaseOrder
            T_PurchaseOrder purchaseOrder = dbTransactionData.PurchaseOrderGetById(purchaseOrderId);
            dbTransactionDataArchive.PurchaseOrderAdd(purchaseOrder);
            dbTransactionData.PurchaseOrderDelete(purchaseOrder);
        }

        private static void ArchiveOperations(IDbTransactionData dbTransactionData,
            IDbTransactionData dbTransactionDataArchive, IAggregator aggregator,
            ProductionOrder productionOrder)
        {
            List<ProductionOrderOperation> operations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            dbTransactionDataArchive.ProductionOrderOperationAddAll(operations);
            dbTransactionData.ProductionOrderOperationDeleteAll(operations);
        }

        private static void ArchiveCustomerOrderPart(IDbTransactionData dbTransactionData,
            IDbTransactionData dbTransactionDataArchive, CustomerOrderPart customerOrderPart)
        {
            // archive customerOrderPart
            Id customerOrderId = new Id(((CustomerOrderPart) customerOrderPart).GetValue()
                .CustomerOrderId);
            dbTransactionDataArchive.DemandsAdd(customerOrderPart);
            dbTransactionData.DemandsDelete(customerOrderPart);

            // archive customerOrder
            T_CustomerOrder customerOrder = dbTransactionData.CustomerOrderGetById(customerOrderId);
            dbTransactionDataArchive.CustomerOrderAdd(customerOrder);
            dbTransactionData.T_CustomerOrderDelete(customerOrder);
        }

        /**
         * inclusive arrows = DemandToProviders/ProviderToDemands
         */
        private static void RemoveAllArrowsAndStockExchangeProviderOnNotFinishedCustomerOrderParts(
            IDbTransactionData dbTransactionData, IAggregator aggregator)
        {
            IDemands copyOfCustomerOrderParts = new Demands();
            copyOfCustomerOrderParts.AddAll(dbTransactionData.CustomerOrderPartGetAll());
            foreach (var customerOrderPart in copyOfCustomerOrderParts)
            {
                if (customerOrderPart.IsFinished() == false)
                {
                    List<ILinkDemandAndProvider> demandAndProviderLinks =
                        aggregator.GetArrowsToAndFrom(customerOrderPart);


                    // remove child (stockExchangeProvider) on COP
                    IProviders stockExchangeProviders =
                        aggregator.GetAllChildProvidersOf(customerOrderPart);
                    if (stockExchangeProviders.Count() > 1)
                    {
                        throw new MrpRunException("A COP can only have one child.");
                    }

                    foreach (var stockExchangeProvider in stockExchangeProviders)
                    {
                        demandAndProviderLinks =
                            aggregator.GetArrowsToAndFrom(stockExchangeProvider);
                        dbTransactionData.DeleteA(stockExchangeProvider);
                    }

                    // remove arrows on COP/stockExchangeProvider
                    dbTransactionData.DeleteAllFrom(demandAndProviderLinks);
                }
            }
        }
    }
}