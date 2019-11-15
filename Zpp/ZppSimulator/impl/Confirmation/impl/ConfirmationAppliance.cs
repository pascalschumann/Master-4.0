using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Microsoft.EntityFrameworkCore.Internal;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.OpenDemand;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.StackSet;

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
            Providers copyOfProductionOrders = new Providers();
            copyOfProductionOrders.AddAll(dbTransactionData.ProductionOrderGetAll());

            foreach (var provider in copyOfProductionOrders)
            {
                ProductionOrder productionOrder = (ProductionOrder) provider;
                State state = productionOrder.DetermineProductionOrderState();
                switch (state)
                {
                    case State.Created:
                        HandleProductionOrderIsInStateCreated(productionOrder, aggregator,
                            dbTransactionData);
                        break;
                    case State.InProgress:
                        HandleProductionOrderIsInProgress();
                        break;
                    case State.Finished:
                        HandleProductionOrderIsFinished(productionOrder, aggregator,
                            dbTransactionData);
                        break;
                    default: throw new MrpRunException("This state is not expected.");
                }
            }


            // Entferne alle Pfeile auf StockExchangeProvider zeigend
            // Entferne alle Pfeile von StockExchangeDemands weg zeigend
            //  --> übrig beleiben Pfeile zwischen StockExchangeProvider und StockExchangeDemands
            List<ILinkDemandAndProvider>
                stockExchangeLinks = new List<ILinkDemandAndProvider>();
            foreach (var stockExchangeDemand in dbTransactionData.StockExchangeDemandsGetAll())
            {
                List<ILinkDemandAndProvider> fromStockExchanges =
                    aggregator.GetArrowsFrom(stockExchangeDemand);
                if (fromStockExchanges != null)
                {
                    stockExchangeLinks.AddRange(fromStockExchanges);
                }
            }

            foreach (var stockExchangeProvider in dbTransactionData.StockExchangeProvidersGetAll())
            {
                List<ILinkDemandAndProvider> toStockExchanges =
                    aggregator.GetArrowsTo(stockExchangeProvider);
                if (toStockExchanges != null)
                {
                    stockExchangeLinks.AddRange(toStockExchanges);
                }
            }
            dbTransactionData.DeleteAllFrom(stockExchangeLinks);

            /*
                foreach sed in beendeten und geschlossenen StockExchangeDemands
                    Archiviere sed und seine parents (StockExchangeProvider) 
                      und die Pfeile dazwischen                
             */
            IStackSet<IDemandOrProvider> demandOrProvidersToArchive =
                new StackSet<IDemandOrProvider>();
            foreach (var stockExchangeDemand in dbTransactionData.StockExchangeDemandsGetAll())
            {
                bool isOpen = OpenDemandManager.IsOpen((StockExchangeDemand) stockExchangeDemand);

                if (isOpen == false && stockExchangeDemand.IsFinished())
                {
                    // parent (StockExchangeProviders)
                    Providers stockExchangeProviders =
                        aggregator.GetAllParentProvidersOf(stockExchangeDemand);
                    foreach (var stockExchangeProvider in stockExchangeProviders)
                    {
                        demandOrProvidersToArchive.Push(stockExchangeProvider);
                    }
                }
            }

            // archive collected stockexchanges
            foreach (var demandOrProviderToArchive in demandOrProvidersToArchive)
            {
                ArchiveDemandOrProvider(demandOrProviderToArchive, dbTransactionData, aggregator,
                    true);
            }

            ArchiveFinishedCustomerOrderPartsAndDeleteTheirArrows(dbTransactionData, aggregator);


            ArchivedCustomerOrdersWithoutCustomerOrderParts();
            ArchivedPurchaseOrdersWithoutPurchaseOrderParts();
        }

        private static void ArchivedCustomerOrdersWithoutCustomerOrderParts()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();


            Ids customerOrderIds = new Ids();
            foreach (var demand in dbTransactionData.CustomerOrderPartGetAll())
            {
                CustomerOrderPart customerOrderPart = (CustomerOrderPart) demand;
                customerOrderIds.Add(customerOrderPart.GetCustomerOrderId());
            }

            foreach (var customerOrder in dbTransactionData.CustomerOrderGetAll())
            {
                bool customerOrderHasNoCustomerOrderPart =
                    customerOrderIds.Contains(customerOrder.GetId()) == false;
                if (customerOrderHasNoCustomerOrderPart)
                {
                    ArchiveCustomerOrder(customerOrder.GetId());
                }
            }
        }

        private static void ArchivedPurchaseOrdersWithoutPurchaseOrderParts()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();


            Ids purchaseOrderIds = new Ids();
            foreach (var demand in dbTransactionData.PurchaseOrderPartGetAll())
            {
                PurchaseOrderPart purchaseOrderPart = (PurchaseOrderPart) demand;
                purchaseOrderIds.Add(purchaseOrderPart.GetPurchaseOrderId());
            }

            foreach (var purchaseOrder in dbTransactionData.PurchaseOrderGetAll())
            {
                bool purchaseOrderHasNoPurchaseOrderParts =
                    purchaseOrderIds.Contains(purchaseOrder.GetId()) == false;
                if (purchaseOrderHasNoPurchaseOrderParts)
                {
                    ArchivePurchaseOrder(purchaseOrder);
                }
            }
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
            if (demandOrProvider == null)
            {
                throw new MrpRunException("Given demandOrProvider cannot be null.");
            }

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
         * - parent (StockExchangeDemand) if includeStockExchanges true
         * - childs (ProductionOrderBoms)
         * - childs of childs (StockExchangeProvider) if includeStockExchanges true
         */
        private static List<IDemandOrProvider> CreateProductionOrderSubGraph(
            bool includeStockExchanges, ProductionOrder productionOrder, IAggregator aggregator)
        {
            List<IDemandOrProvider> demandOrProvidersOfProductionOrderSubGraph =
                new List<IDemandOrProvider>();
            demandOrProvidersOfProductionOrderSubGraph.Add(productionOrder);

            if (includeStockExchanges)
            {
                Demands stockExchangeDemands = aggregator.GetAllParentDemandsOf(productionOrder);
                if (stockExchangeDemands.Count() > 1)
                {
                    throw new MrpRunException(
                        "A productionOrder can only have one parentDemand (stockExchangeDemand).");
                }

                demandOrProvidersOfProductionOrderSubGraph.AddRange(stockExchangeDemands);
            }

            Demands productionOrderBoms = aggregator.GetAllChildDemandsOf(productionOrder);
            demandOrProvidersOfProductionOrderSubGraph.AddRange(productionOrderBoms);

            if (includeStockExchanges)
            {
                foreach (var productionOrderBom in productionOrderBoms)
                {
                    Providers stockExchangeProvider =
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
            IOpenDemandManager openDemandManager =
                ZppConfiguration.CacheManager.GetOpenDemandManager();
            foreach (var demandOrProvider in demandOrProvidersToDelete)
            {
                // don't forget to delete it from openDemands
                if (demandOrProvider.GetType() == typeof(StockExchangeDemand))
                {
                    if (openDemandManager.Contains((Demand) demandOrProvider))
                    {
                        openDemandManager.RemoveDemand((Demand) demandOrProvider);
                    }
                }

                List<ILinkDemandAndProvider> demandAndProviders =
                    aggregator.GetArrowsToAndFrom(demandOrProvider); // TODO: why
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
            ArchiveOperations(dbTransactionData, dbTransactionDataArchive, aggregator,
                productionOrder);

            // collect demands Or providers
            List<IDemandOrProvider> demandOrProvidersToArchive =
                CreateProductionOrderSubGraph(false, productionOrder, aggregator);

            // delete & archive all collected entities
            foreach (var demandOrProvider in demandOrProvidersToArchive)
            {
                // arrow outside mus be removed
                List<ILinkDemandAndProvider> demandAndProviderLinks;
                if (demandOrProvider.GetType() == typeof(ProductionOrder))
                {
                    // archive only link from ProductionOrder
                    demandAndProviderLinks = aggregator.GetArrowsFrom(demandOrProvider);
                    dbTransactionDataArchive.AddAllFrom(demandAndProviderLinks);
                    dbTransactionData.DeleteAllFrom(demandAndProviderLinks);
                    demandAndProviderLinks = aggregator.GetArrowsTo(demandOrProvider);
                    dbTransactionData.DeleteAllFrom(demandAndProviderLinks);
                }
                else if (demandOrProvider.GetType() == typeof(ProductionOrderBom))
                {
                    // archive only link to ProductionOrderBom
                    demandAndProviderLinks = aggregator.GetArrowsTo(demandOrProvider);
                    dbTransactionDataArchive.AddAllFrom(demandAndProviderLinks);
                    dbTransactionData.DeleteAllFrom(demandAndProviderLinks);
                    demandAndProviderLinks = aggregator.GetArrowsFrom(demandOrProvider);
                    dbTransactionData.DeleteAllFrom(demandAndProviderLinks);
                }
                else
                {
                    throw new MrpRunException("In this case not possible");
                }


                dbTransactionDataArchive.AddA(demandOrProvider);
                dbTransactionData.DeleteA(demandOrProvider);
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
            dbTransactionData.CustomerOrderDelete(customerOrder);
        }

        /**
         * inclusive arrows = DemandToProviders/ProviderToDemands
         */
        private static void RemoveAllArrowsAndStockExchangeProviderOnNotFinishedCustomerOrderParts(
            IDbTransactionData dbTransactionData, IAggregator aggregator)
        {
            Demands copyOfCustomerOrderParts = new Demands();
            copyOfCustomerOrderParts.AddAll(dbTransactionData.CustomerOrderPartGetAll());
            foreach (var customerOrderPart in copyOfCustomerOrderParts)
            {
                if (customerOrderPart.IsFinished() == false)
                {
                    List<ILinkDemandAndProvider> demandAndProviderLinks =
                        aggregator.GetArrowsToAndFrom(customerOrderPart);


                    // remove child (stockExchangeProvider) on COP
                    Providers stockExchangeProviders =
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

        private static void ArchiveFinishedCustomerOrderPartsAndDeleteTheirArrows(
            IDbTransactionData dbTransactionData, IAggregator aggregator)
        {
            Demands copyOfCustomerOrderParts = new Demands();
            copyOfCustomerOrderParts.AddAll(dbTransactionData.CustomerOrderPartGetAll());
            foreach (var demand in copyOfCustomerOrderParts)
            {
                CustomerOrderPart customerOrderPart = (CustomerOrderPart) demand;
                if (customerOrderPart.IsFinished())
                {
                    ArchiveCustomerOrder(customerOrderPart.GetCustomerOrderId());
                    // archive cop
                    List<ILinkDemandAndProvider> arrows =
                        aggregator.GetArrowsFrom(customerOrderPart);
                    dbTransactionData.DeleteAllFrom(arrows);
                    ArchiveDemandOrProvider(customerOrderPart, dbTransactionData, aggregator,
                        false);
                }
            }
        }

        private static void ArchiveCustomerOrder(Id customerOrderId)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();
            T_CustomerOrder customerOrder = dbTransactionData.CustomerOrderGetById(customerOrderId);
            dbTransactionDataArchive.CustomerOrderAdd(customerOrder);
            dbTransactionData.CustomerOrderDelete(customerOrder);
        }

        private static void ArchivePurchaseOrder(T_PurchaseOrder purchaseOrder)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();
            dbTransactionDataArchive.PurchaseOrderAdd(purchaseOrder);
            dbTransactionData.PurchaseOrderDelete(purchaseOrder);
        }
    }
}