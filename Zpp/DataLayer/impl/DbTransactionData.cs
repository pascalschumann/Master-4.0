using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Akka.Util.Internal;
using Master40.DB;
using Master40.DB.Data.Context;
using Master40.DB.Data.Helper;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Microsoft.EntityFrameworkCore;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.NodeManagement;
using Zpp.Utils;
using Zpp.WrappersForCollections;

namespace Zpp.DbCache
{
    /**
     * NOTE: TransactionData does NOT include CustomerOrders or CustomerOrderParts !
     */
    public class DbTransactionData : IDbTransactionData
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ProductionDomainContext _productionDomainContext;

        private readonly IDbMasterDataCache _dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();

        // TODO: These 3 lines should be removed
        private readonly List<M_Article> _articles;
        private readonly List<M_ArticleBom> _articleBoms;
        private readonly List<M_BusinessPartner> _businessPartners;

        // T_*

        // demands
        private readonly ProductionOrderBoms _productionOrderBoms;

        // demands
        private readonly StockExchangeDemands _stockExchangeDemands;

        // providers
        private readonly StockExchangeProviders _stockExchangeProviders;

        // providers
        private readonly PurchaseOrderParts _purchaseOrderParts;

        // providers
        private readonly ProductionOrders _productionOrders;

        // others
        private List<T_PurchaseOrder> _purchaseOrders;
        private readonly ProductionOrderOperations _productionOrderOperations;

        private readonly CustomerOrderParts _customerOrderParts;

        private readonly CustomerOrders _customerOrders;

        private readonly DemandToProviderTable _demandToProviderTable;

        private readonly ProviderToDemandTable _providerToDemandTable;

        public DbTransactionData(ProductionDomainContext productionDomainContext)
        {
            _productionDomainContext = productionDomainContext;

            // cache tables
            // TODO: These 3 lines should be removed
            _businessPartners = _productionDomainContext.BusinessPartners.ToList();
            _articleBoms = _productionDomainContext.ArticleBoms.Include(m => m.ArticleChild)
                .ToList();
            _articles = _productionDomainContext.Articles.Include(m => m.ArticleBoms)
                .ThenInclude(m => m.ArticleChild).Include(m => m.ArticleBoms)
                .ThenInclude(x => x.Operation).ThenInclude(x => x.ResourceSkill)
                .ThenInclude(s => s.ResourceSetups).ThenInclude(r => r.Resource)
                .Include(x => x.ArticleToBusinessPartners).ThenInclude(x => x.BusinessPartner)
                .ToList();

            _productionOrderBoms =
                new ProductionOrderBoms(_productionDomainContext.ProductionOrderBoms.ToList());

            _stockExchangeDemands =
                new StockExchangeDemands(_productionDomainContext.StockExchanges.ToList());
            _stockExchangeProviders =
                new StockExchangeProviders(_productionDomainContext.StockExchanges.ToList());

            _productionOrders =
                new ProductionOrders(_productionDomainContext.ProductionOrders.ToList());
            _purchaseOrderParts =
                new PurchaseOrderParts(_productionDomainContext.PurchaseOrderParts.ToList());

            _customerOrderParts =
                new CustomerOrderParts(_productionDomainContext.CustomerOrderParts.ToList());
            _customerOrders = new CustomerOrders(_productionDomainContext.CustomerOrders.ToList());

            // others
            _purchaseOrders = _productionDomainContext.PurchaseOrders.ToList();
            _productionOrderOperations = new ProductionOrderOperations(
                _productionDomainContext.ProductionOrderOperations.ToList());

            // demandToProvider

            _demandToProviderTable =
                new DemandToProviderTable(_productionDomainContext.DemandToProviders.ToList());
            _providerToDemandTable =
                new ProviderToDemandTable(_productionDomainContext.ProviderToDemand.ToList());
        }

        public List<M_BusinessPartner> M_BusinessPartnerGetAll()
        {
            return _businessPartners;
        }

        public M_ArticleBom M_ArticleBomGetById(Id id)
        {
            return _articleBoms.Single(x => x.Id == id.GetValue());
        }

        public M_Article M_ArticleGetById(Id id)
        {
            return _articles.Single(x => x.Id == id.GetValue());
        }

        public void DemandToProvidersRemoveAll()
        {
            _productionDomainContext.DemandToProviders.RemoveRange(_productionDomainContext
                .DemandToProviders);
        }

        public void PersistDbCache()
        {
            // TODO: performance issue: Batch insert, since those T_* didn't exist before anyways, update is useless
            // TODO: SaveChanges at the end only once

            // first collect all T_* entities
            List<T_ProductionOrderBom> tProductionOrderBoms =
                _productionOrderBoms.GetAllAs<T_ProductionOrderBom>();
            List<T_StockExchange> tStockExchangeDemands =
                _stockExchangeDemands.GetAllAs<T_StockExchange>();
            List<T_StockExchange> tStockExchangesProviders =
                _stockExchangeProviders.GetAllAs<T_StockExchange>();
            List<T_ProductionOrder> tProductionOrders =
                _productionOrders.GetAllAs<T_ProductionOrder>();
            List<T_ProductionOrderOperation> tProductionOrderOperations =
                _productionOrderOperations.GetAllAsT_ProductionOrderOperation();
            List<T_PurchaseOrderPart> tPurchaseOrderParts =
                _purchaseOrderParts.GetAllAs<T_PurchaseOrderPart>();
            List<T_CustomerOrderPart> tCustomerOrderParts =
                _customerOrderParts.GetAllAs<T_CustomerOrderPart>();
            List<T_CustomerOrder> tCustomerOrders = _customerOrders.GetAllAsTCustomerOrders();


            // Insert all T_* entities
            InsertOrUpdateRange(tProductionOrders, _productionDomainContext.ProductionOrders);
            InsertOrUpdateRange(tProductionOrderOperations,
                _productionDomainContext.ProductionOrderOperations);
            InsertOrUpdateRange(tProductionOrderBoms, _productionDomainContext.ProductionOrderBoms);
            InsertOrUpdateRange(tStockExchangeDemands, _productionDomainContext.StockExchanges);
            InsertOrUpdateRange(tCustomerOrders, _productionDomainContext.CustomerOrders);
            InsertOrUpdateRange(tCustomerOrderParts, _productionDomainContext.CustomerOrderParts);

            // providers
            InsertOrUpdateRange(tStockExchangesProviders, _productionDomainContext.StockExchanges);
            InsertOrUpdateRange(tPurchaseOrderParts, _productionDomainContext.PurchaseOrderParts);
            InsertOrUpdateRange(_purchaseOrders, _productionDomainContext.PurchaseOrders);

            // at the end: T_DemandToProvider & T_ProviderToDemand
            InsertOrUpdateRange(DemandToProviderGetAll(),
                _productionDomainContext.DemandToProviders);
            if (ProviderToDemandGetAll().Any())
            {
                InsertOrUpdateRange(ProviderToDemandGetAll(),
                    _productionDomainContext.ProviderToDemand);
            }

            try
            {
                _productionDomainContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Error("DbCache could not be persisted.");
                throw e;
            }
        }

        private void InsertOrUpdateRange<TEntity>(IEnumerable<TEntity> entities,
            DbSet<TEntity> dbSet) where TEntity : BaseEntity
        {
            // dbSet.AddRange(entities);
            foreach (var entity in entities)
            {
                // e.g. if it is a PrBom which is toPurchase
                if (entity != null)
                {
                    InsertOrUpdate(entity, dbSet);
                }
            }
        }

        private void InsertOrUpdate<TEntity>(TEntity entity, DbSet<TEntity> dbSet)
            where TEntity : BaseEntity
        {
            TEntity foundEntity = dbSet.Find(entity.Id);
            if (foundEntity == null
                ) // TODO: performance issue: a select before every insert is a no go
                // it's not in DB yet
            {
                _productionDomainContext.Entry(entity).State = EntityState.Added;
                dbSet.Add(entity);
            }
            else
                // it's already in DB
            {
                CopyDbPropertiesTo(entity, foundEntity);
                _productionDomainContext.Entry(foundEntity).State = EntityState.Modified;
                dbSet.Update(foundEntity);
            }
        }

        public static void CopyDbPropertiesTo<T>(T source, T dest)
        {
            DbUtils.CopyDbPropertiesTo(source, dest);
        }

        public void DemandsAdd(Demand demand)
        {
            if (demand.GetType() == typeof(ProductionOrderBom))
            {
                _productionOrderBoms.Add((ProductionOrderBom) demand);
            }
            else if (demand.GetType() == typeof(StockExchangeDemand))
            {
                _stockExchangeDemands.Add((StockExchangeDemand) demand);
            }
            else
            {
                Logger.Error("Unknown type implementing Demand");
            }
        }

        public void ProvidersAdd(Provider provider)
        {
            if (provider.GetType() == typeof(ProductionOrder))
            {
                _productionOrders.Add((ProductionOrder) provider);
            }
            else if (provider.GetType() == typeof(PurchaseOrderPart))
            {
                _purchaseOrderParts.Add((PurchaseOrderPart) provider);
            }
            else if (provider.GetType() == typeof(StockExchangeProvider))
            {
                _stockExchangeProviders.Add((StockExchangeProvider) provider);
            }
            else
            {
                Logger.Error("Unknown type implementing IProvider");
            }
        }

        public IDemands DemandsGetAll()
        {
            Demands demands = new Demands();

            if (_productionOrderBoms.Any())
            {
                demands.AddAll(_productionOrderBoms);
            }

            if (_stockExchangeDemands.Any())
            {
                demands.AddAll(_stockExchangeDemands);
            }

            if (_customerOrderParts.Any())
            {
                demands.AddAll(_customerOrderParts);
            }

            return demands;
        }

        public IProviders ProvidersGetAll()
        {
            IProviders providers = new Providers();
            providers.AddAll(_productionOrders);
            providers.AddAll(_purchaseOrderParts);
            providers.AddAll(_stockExchangeProviders);
            return providers;
        }

        public ProductionOrderBoms ProductionOrderBomGetAll()
        {
            return _productionOrderBoms;
        }

        public StockExchangeProviders StockExchangeProvidersGetAll()
        {
            return _stockExchangeProviders;
        }

        public PurchaseOrderParts PurchaseOrderPartGetAll()
        {
            return _purchaseOrderParts;
        }

        public ProductionOrders ProductionOrderGetAll()
        {
            return _productionOrders;
        }

        public void DemandsAddAll(IDemands demands)
        {
            foreach (var demand in demands)
            {
                DemandsAdd(demand);
            }

            // T_ProductionOrderOperation
            IStackSet<ProductionOrderOperation> tProductionOrderOperations =
                new StackSet<ProductionOrderOperation>();
            foreach (var productionOrderBom in _productionOrderBoms)
            {
                T_ProductionOrderBom tProductionOrderBom =
                    (T_ProductionOrderBom) productionOrderBom.ToIDemand();
                if (tProductionOrderBom != null)
                {
                    tProductionOrderOperations.Push(new ProductionOrderOperation(
                        tProductionOrderBom.ProductionOrderOperation));
                }
            }

            _productionOrderOperations.AddAll(tProductionOrderOperations);
        }

        public void ProvidersAddAll(IProviders providers)
        {
            foreach (var provider in providers)
            {
                ProvidersAdd(provider);
            }

            // T_PurchaseOrders
            foreach (var tPurchaseOrderPart in _purchaseOrderParts.GetAllAs<T_PurchaseOrderPart>())
            {
                _purchaseOrders.Add(tPurchaseOrderPart.PurchaseOrder);
            }
        }

        public IDemandToProviderTable DemandToProviderGetAll()
        {
            return _demandToProviderTable;
        }

        public Demand DemandsGetById(Id id)
        {
            return DemandsGetAll().GetAll().Find(x => x.GetId().Equals(id));
        }

        public Provider ProvidersGetById(Id id)
        {
            return ProvidersGetAll().GetAll().Find(x => x.GetId().Equals(id));
        }

        public IProviderToDemandTable ProviderToDemandGetAll()
        {
            return _providerToDemandTable;
        }

        public T_PurchaseOrder PurchaseOrderGetById(Id id)
        {
            return _purchaseOrders.Single(x => x.Id.Equals(id.GetValue()));
        }

        public List<T_PurchaseOrder> PurchaseOrderGetAll()
        {
            return _purchaseOrders;
        }

        public ProductionOrderOperation ProductionOrderOperationGetById(Id id)
        {
            return _productionOrderOperations.GetAll().SingleOrDefault(x => x.GetId().Equals(id));
        }

        public ProductionOrderOperations ProductionOrderOperationGetAll()
        {
            return _productionOrderOperations;
        }

        public StockExchangeDemands StockExchangeDemandsGetAll()
        {
            return _stockExchangeDemands;
        }

        public ProductionOrder ProductionOrderGetById(Id id)
        {
            return new ProductionOrder(_productionOrders.GetAllAs<T_ProductionOrder>()
                .Single(x => x.GetId().Equals(id)));
        }

        public T_CustomerOrder T_CustomerOrderGetById(Id id)
        {
            return _customerOrders.Single(x => x.Id.Equals(id.GetValue()));
        }

        public List<T_CustomerOrder> T_CustomerOrderGetAll()
        {
            return _customerOrders.GetAll();
        }

        public Demands T_CustomerOrderPartGetAll()
        {
            List<Demand> demands = new List<Demand>();
            foreach (var demand in _customerOrderParts)
            {
                demands.Add(new CustomerOrderPart(demand.ToIDemand()));
            }

            return new Demands(demands);
        }

        public void CustomerOrderPartAdd(T_CustomerOrderPart customerOrderPart)
        {
            _customerOrderParts.Add(new CustomerOrderPart(customerOrderPart));
        }

        public void DemandToProviderAdd(T_DemandToProvider demandToProvider)
        {
            _demandToProviderTable.Add(demandToProvider);
        }

        public void ProviderToDemandAddAll(ProviderToDemandTable providerToDemandTable)
        {
            _providerToDemandTable.AddAll(providerToDemandTable);
        }

        public void Dispose()
        {
            _articles.Clear();
            _articleBoms.Clear();
            _businessPartners.Clear();
            _customerOrders.Clear();
            _productionOrders.Clear();
            _purchaseOrders.Clear();
            _customerOrderParts.Clear();
            _productionOrderBoms.Clear();
            _productionOrderOperations.Clear();
            _purchaseOrderParts.Clear();
            _stockExchangeDemands.Clear();
            _stockExchangeProviders.Clear();
            _demandToProviderTable.Clear();
            _providerToDemandTable.Clear();
        }
    }
}