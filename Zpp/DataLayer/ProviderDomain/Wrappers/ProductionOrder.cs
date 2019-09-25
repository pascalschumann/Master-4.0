using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp.ProductionManagement;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.ProviderDomain.Wrappers
{
    /**
     * wraps T_ProductionOrder
     */
    public class ProductionOrder : Provider
    {
        private readonly T_ProductionOrder _productionOrder;
        public ProductionOrder(IProvider provider) : base(
            provider)
        {
            _productionOrder = (T_ProductionOrder)provider;
        }

        public override IProvider ToIProvider()
        {
            return (T_ProductionOrder) _provider;
        }

        public override Id GetArticleId()
        {
            Id articleId = new Id(((T_ProductionOrder) _provider).ArticleId);
            return articleId;
        }

        public override void CreateDependingDemands(M_Article article,
            Provider parentProvider, Quantity demandedQuantity)
        {
            _dependingDemands = ProductionManager.CreateProductionOrderBoms(article,
                parentProvider, demandedQuantity);
        }

        public override DueTime GetDueTime()
        {
            T_ProductionOrder productionOrder = (T_ProductionOrder) _provider;
            return new DueTime(productionOrder.DueTime);
        }

        public override DueTime GetStartTime()
        {
            return GetDueTime();
        }

        public ProductionOrderBoms GetProductionOrderBoms()
        {
            return ZppConfiguration.CacheManager.GetAggregator().GetProductionOrderBomsOfProductionOrder(this);
        }

        public bool HasOperations()
        {
            ICacheManager cacheManager = ZppConfiguration.CacheManager;
            List<ProductionOrderOperation> productionOrderOperations = cacheManager
                .GetAggregator().GetProductionOrderOperationsOfProductionOrder(this);
            if (productionOrderOperations == null)
            {
                return false;
            }

            return productionOrderOperations.Any();
        }
        
        public override void SetProvided(DueTime atTime)
        {
            throw new System.NotImplementedException();
        }

        public override void SetStartTime(DueTime dueTime)
        {
            _productionOrder.DueTime = dueTime.GetValue();
        }
    }
}