using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
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

        public override DueTime GetDueTime()
        {
            return GetEndTime();
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

        public override void SetStartTime(DueTime startTime)
        {
            _productionOrder.DueTime = startTime.GetValue();
        }

        public override void SetDone()
        {
            throw new System.NotImplementedException();
        }

        public override void SetInProgress()
        {
            throw new System.NotImplementedException();
        }
        
        public override Duration GetDuration()
        {
            return Duration.Null();
        }

        public override DueTime GetEndTime()
        {
            return new DueTime(_productionOrder.DueTime);
        }
    }
}