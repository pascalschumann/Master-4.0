using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB;
using Master40.DB.Data.Context;
using Master40.DB.Data.Helper;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Microsoft.EntityFrameworkCore;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.StackSet;

namespace Zpp.DataLayer.impl
{
    /**
     * NOTE: TransactionData does NOT include CustomerOrders or CustomerOrderParts !
     */
    public class DbTransactionData : IDbTransactionData
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ProductionDomainContext _productionDomainContext;

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
        private readonly IStackSet<T_PurchaseOrder> _purchaseOrders = new StackSet<T_PurchaseOrder>();
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
            _purchaseOrders.PushAll(_productionDomainContext.PurchaseOrders.ToList());
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

        internal void PersistDbCache()
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
            InsertOrUpdateRange(tProductionOrders, _productionDomainContext.ProductionOrders, _productionDomainContext);
            InsertOrUpdateRange(tProductionOrderOperations,
                _productionDomainContext.ProductionOrderOperations, _productionDomainContext);
            InsertOrUpdateRange(tProductionOrderBoms, _productionDomainContext.ProductionOrderBoms, _productionDomainContext);
            InsertOrUpdateRange(tStockExchangeDemands, _productionDomainContext.StockExchanges, _productionDomainContext);
            InsertOrUpdateRange(tCustomerOrders, _productionDomainContext.CustomerOrders, _productionDomainContext);
            InsertOrUpdateRange(tCustomerOrderParts, _productionDomainContext.CustomerOrderParts, _productionDomainContext);

            // providers
            InsertOrUpdateRange(tStockExchangesProviders, _productionDomainContext.StockExchanges, _productionDomainContext);
            InsertOrUpdateRange(tPurchaseOrderParts, _productionDomainContext.PurchaseOrderParts, _productionDomainContext);
            InsertOrUpdateRange(_purchaseOrders, _productionDomainContext.PurchaseOrders, _productionDomainContext);

            // at the end: T_DemandToProvider & T_ProviderToDemand
            InsertOrUpdateRange(DemandToProviderGetAll(),
                _productionDomainContext.DemandToProviders, _productionDomainContext);
            if (ProviderToDemandGetAll().Any())
            {
                InsertOrUpdateRange(ProviderToDemandGetAll(),
                    _productionDomainContext.ProviderToDemand, _productionDomainContext);
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

        public void CustomerOrderAdd(T_CustomerOrder customerOrder)
        {
            _customerOrders.Add(customerOrder);
        }

        public static void InsertRange<TEntity>(IEnumerable<TEntity> entities,
            DbSet<TEntity> dbSet, ProductionDomainContext productionDomainContext)
            where TEntity : BaseEntity
        {
            foreach (var entity in entities)
            {
                // e.g. if it is a PrBom which is toPurchase
                if (entity != null)
                {
                    Insert(entity, dbSet, productionDomainContext);
                }
            }
        }

        public static void InsertOrUpdateRange<TEntity>(IEnumerable<TEntity> entities,
            DbSet<TEntity> dbSet, ProductionDomainContext productionDomainContext) where TEntity : BaseEntity
        {
            // dbSet.AddRange(entities);
            foreach (var entity in entities)
            {
                // e.g. if it is a PrBom which is toPurchase
                if (entity != null)
                {
                    InsertOrUpdate(entity, dbSet, productionDomainContext);
                }
            }
        }

        private static void Insert<TEntity>(TEntity entity, DbSet<TEntity> dbSet,
            ProductionDomainContext productionDomainContext) where TEntity : BaseEntity
        {
            productionDomainContext.Entry(entity).State = EntityState.Added;
            dbSet.Add(entity);
        }

        private static void InsertOrUpdate<TEntity>(TEntity entity, DbSet<TEntity> dbSet, ProductionDomainContext productionDomainContext)
            where TEntity : BaseEntity
        {
            TEntity foundEntity = dbSet.Find(entity.Id);
            if (foundEntity == null
                ) // TODO: performance issue: a select before every insert is a no go
                // it's not in DB yet
            {
                productionDomainContext.Entry(entity).State = EntityState.Added;
                dbSet.Add(entity);
            }
            else
                // it's already in DB
            {
                CopyDbPropertiesTo(entity, foundEntity);
                productionDomainContext.Entry(foundEntity).State = EntityState.Modified;
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
            else if (demand.GetType() == typeof(CustomerOrderPart))
            {
                _customerOrderParts.Add((CustomerOrderPart)demand);
            }
            else
            {
                throw new MrpRunException("This type is unkown.");
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
                throw new MrpRunException("This type is not known.");
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
                    ((ProductionOrderBom) productionOrderBom).EnsureOperationIsLoadedIfExists();
                    if (tProductionOrderBom.ProductionOrderOperation == null)
                    {
                        throw new MrpRunException(
                            "Every tProductionOrderBom must have an operation.");
                    }

                    tProductionOrderOperations.Push(new ProductionOrderOperation(
                        tProductionOrderBom.ProductionOrderOperation));
                }
            }

            _productionOrderOperations.AddAll(tProductionOrderOperations);
        }

        public void ProductionOrderOperationAdd(
            ProductionOrderOperation productionOrderOperation)
        {
            // this (productionOrderOperation was already added) can happen,
            // since an operation can be used multiple times for ProductionorderBoms
            if (_productionOrderOperations.Contains(productionOrderOperation) == false)
            {
                _productionOrderOperations.Add(productionOrderOperation);
            }
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
                PurchaseOrderAdd(tPurchaseOrderPart.PurchaseOrder);
            }
        }

        public void PurchaseOrderAdd(T_PurchaseOrder purchaseOrder)
        {
            _purchaseOrders.Push(purchaseOrder);
        }

        public void PurchaseOrderDelete(T_PurchaseOrder purchaseOrder)
        {
            _purchaseOrders.Remove(purchaseOrder);
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
            try
            {
                return _purchaseOrders.Single(x => x.Id.Equals(id.GetValue()));
            }
            catch (InvalidOperationException e)
            {
                return null;
            }
            
        }

        public List<T_PurchaseOrder> PurchaseOrderGetAll()
        {
            return _purchaseOrders.GetAll();
        }

        public ProductionOrderOperation ProductionOrderOperationGetById(Id id)
        {
            try
            {
                return _productionOrderOperations.GetAll()
                    .SingleOrDefault(x => x.GetId().Equals(id));
            }
            catch (InvalidOperationException e)
            {
            }

            return null;
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

        public T_CustomerOrder CustomerOrderGetById(Id id)
        {
            return _customerOrders.Single(x => x.Id.Equals(id.GetValue()));
        }

        public List<T_CustomerOrder> CustomerOrderGetAll()
        {
            return _customerOrders.GetAll();
        }

        public Demands CustomerOrderPartGetAll()
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

        public void AddAllFrom(EntityCollector otherEntityCollector)
        {
            if (otherEntityCollector.GetDemands().Any())
            {
                DemandsAddAll(otherEntityCollector.GetDemands());
            }

            if (otherEntityCollector.GetProviders().Any())
            {
                ProvidersAddAll(otherEntityCollector.GetProviders());
            }

            if (otherEntityCollector.GetDemandToProviderTable().Any())
            {
                _demandToProviderTable.AddAll(otherEntityCollector.GetDemandToProviderTable());
            }

            if (otherEntityCollector.GetProviderToDemandTable().Any())
            {
                _providerToDemandTable.AddAll(otherEntityCollector.GetProviderToDemandTable());
            }
        }

        public void StockExchangeProvidersDelete(StockExchangeProvider stockExchangeProvider)
        {
            _stockExchangeProviders.Remove(stockExchangeProvider);
        }

        public void DemandToProviderDelete(T_DemandToProvider demandToProvider)
        {
            _demandToProviderTable.Remove(demandToProvider);
        }

        public void ProviderToDemandDelete(T_ProviderToDemand providerToDemand)
        {
            _providerToDemandTable.Remove(providerToDemand);
        }

        public void DemandToProviderDeleteAll(IEnumerable<T_DemandToProvider> demandToProviders)
        {
            foreach (var demandToProvider in demandToProviders)
            {
                DemandToProviderDelete(demandToProvider);
            }
        }

        public void ProviderToDemandDeleteAll(IEnumerable<T_ProviderToDemand> providerToDemands)
        {
            foreach (var providerToDemand in providerToDemands)
            {
                ProviderToDemandDelete(providerToDemand);
            }
        }

        public void DeleteA(IDemandOrProvider demandOrProvider)
        {
            if (demandOrProvider is Demand)
            {
                DemandsDelete((Demand) demandOrProvider);
            }
            else if (demandOrProvider is Provider)
            {
                ProvidersDelete((Provider) demandOrProvider);
            }
            else
            {
                throw new MrpRunException("This type is unknown.");
            }
        }

        public void DemandsDelete(Demand demand)
        {
            if (demand.GetType() == typeof(ProductionOrderBom))
            {
                _productionOrderBoms.Remove((ProductionOrderBom) demand);
            }
            else if (demand.GetType() == typeof(StockExchangeDemand))
            {
                _stockExchangeDemands.Remove((StockExchangeDemand) demand);
            }
            else if (demand.GetType() == typeof(CustomerOrderPart))
            {
                _customerOrderParts.Remove((CustomerOrderPart)demand);
            }
            else
            {
                throw new MrpRunException("This type is unknown.");
            }
        }

        public void ProvidersDelete(Provider provider)
        {
            if (provider.GetType() == typeof(ProductionOrder))
            {
                _productionOrders.Remove((ProductionOrder) provider);
            }
            else if (provider.GetType() == typeof(PurchaseOrderPart))
            {
                _purchaseOrderParts.Remove((PurchaseOrderPart) provider);
            }
            else if (provider.GetType() == typeof(StockExchangeProvider))
            {
                _stockExchangeProviders.Remove((StockExchangeProvider) provider);
            }
            else
            {
                throw new MrpRunException("This type is unknown.");
            }
        }

        public void ProductionOrderOperationDeleteAll(
            List<ProductionOrderOperation> productionOrderOperations)
        {
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                _productionOrderOperations.Remove(productionOrderOperation);
            }
        }

        public void ProductionOrderOperationDelete(
            ProductionOrderOperation productionOrderOperation)
        {
            _productionOrderOperations.Remove(productionOrderOperation);
        }

        public void ProductionOrderOperationAddAll(
            List<ProductionOrderOperation> productionOrderOperations)
        {
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                ProductionOrderOperationAdd(productionOrderOperation);
            }
        }

        public void DeleteAllFrom(List<IDemandOrProvider> demandOrProviders)
        {
            foreach (var demandOrProvider in demandOrProviders)
            {
                DeleteA(demandOrProvider);
            }
        }

        public void DeleteAllFrom(List<ILinkDemandAndProvider> demandAndProviderLinks)
        {
            foreach (var demandAndProviderLink in demandAndProviderLinks)
            {
                DeleteA(demandAndProviderLink);
            }
        }

        public void AddAllFrom(List<IDemandOrProvider> demandOrProviders)
        {
            foreach (var demandOrProvider in demandOrProviders)
            {
                AddA(demandOrProvider);
            }
        }

        public void AddA(IDemandOrProvider demandOrProvider)
        {
            if (demandOrProvider is Demand)
            {
                DemandsAdd((Demand) demandOrProvider);
            }
            else if (demandOrProvider is Provider)
            {
                ProvidersAdd((Provider) demandOrProvider);
            }
            else
            {
                throw new MrpRunException("This type is not expected.");
            }
        }

        public void AddAllFrom(List<ILinkDemandAndProvider> demandOrProviders)
        {
            foreach (var demandOrProvider in demandOrProviders)
            {
                AddA(demandOrProvider);
            }
        }

        public void AddA(ILinkDemandAndProvider demandAndProviderLink)
        {
            if (demandAndProviderLink.GetType() == typeof(T_DemandToProvider))
            {
                DemandToProviderAdd((T_DemandToProvider) demandAndProviderLink);
            }
            else if (demandAndProviderLink.GetType() == typeof(T_ProviderToDemand))
            {
                ProviderToDemandAdd((T_ProviderToDemand) demandAndProviderLink);
            }
            else
            {
                throw new MrpRunException("This type is not expected.");
            }
        }

        public void DeleteA(ILinkDemandAndProvider demandAndProviderLink)
        {
            if (demandAndProviderLink.GetType() == typeof(T_DemandToProvider))
            {
                DemandToProviderDelete((T_DemandToProvider) demandAndProviderLink);
            }
            else if (demandAndProviderLink.GetType() == typeof(T_ProviderToDemand))
            {
                ProviderToDemandDelete((T_ProviderToDemand) demandAndProviderLink);
            }
            else
            {
                throw new MrpRunException("This type is not expected.");
            }
        }

        public void ProviderToDemandAdd(T_ProviderToDemand providerToDemand)
        {
            _providerToDemandTable.Add(providerToDemand);
        }

        public override string ToString()
        {
            string result = "";

            result += "_customerOrders:" + Environment.NewLine + _customerOrders.ToString() +
                      Environment.NewLine + Environment.NewLine + Environment.NewLine;
            result += "_customerOrderParts:" + Environment.NewLine +
                      _customerOrderParts.ToString() + Environment.NewLine + Environment.NewLine +
                      Environment.NewLine;
            result += "_demandToProviderTable:" + Environment.NewLine +
                      _demandToProviderTable.ToString() + Environment.NewLine +
                      Environment.NewLine + Environment.NewLine;
            result += "_productionOrderBoms:" + Environment.NewLine +
                      _productionOrderBoms.ToString() + Environment.NewLine + Environment.NewLine +
                      Environment.NewLine;
            result += "_productionOrderOperations:" + Environment.NewLine +
                      _productionOrderOperations.ToString() + Environment.NewLine +
                      Environment.NewLine + Environment.NewLine;
            result += "_productionOrders:" + Environment.NewLine + _productionOrders.ToString() +
                      Environment.NewLine + Environment.NewLine + Environment.NewLine;
            result += "_providerToDemandTable:" + Environment.NewLine +
                      _providerToDemandTable.ToString() + Environment.NewLine +
                      Environment.NewLine + Environment.NewLine;
            result += "_purchaseOrderParts:" + Environment.NewLine +
                      _purchaseOrderParts.ToString() + Environment.NewLine + Environment.NewLine +
                      Environment.NewLine;
            result += "_purchaseOrders:" + Environment.NewLine + _purchaseOrders.ToString() +
                      Environment.NewLine + Environment.NewLine + Environment.NewLine;
            result += "_stockExchangeDemands:" + Environment.NewLine +
                      _stockExchangeDemands.ToString() + Environment.NewLine + Environment.NewLine +
                      Environment.NewLine;
            result += "_stockExchangeProviders:" + Environment.NewLine +
                      _stockExchangeProviders.ToString() + Environment.NewLine +
                      Environment.NewLine + Environment.NewLine;

            return result;
        }

        public void T_CustomerOrderDelete(T_CustomerOrder customerOrder)
        {
            _customerOrders.Remove(customerOrder);
        }
    }
}