using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.Util;

namespace Zpp.DataLayer.impl.ProviderDomain.Wrappers
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

        public override void SetStartTimeBackward(DueTime startTime)
        {
            if (_productionOrder.IsReadOnly)
            {
                throw new MrpRunException("A readOnly entity cannot be changed anymore.");
            }
            _productionOrder.DueTime = startTime.GetValue();
        }

        public override void SetFinished()
        {
            // has no state
            throw new System.NotImplementedException();
        }

        public override void SetInProgress()
        {
            // has no state
            throw new System.NotImplementedException();
        }
        
        public override Duration GetDuration()
        {
            return Duration.Null();
        }

        public override DueTime GetEndTimeBackward()
        {
            return new DueTime(_productionOrder.DueTime);
        }

        public override bool IsFinished()
        {
            // has no state --> always true
            return true;
        }

        public override void SetEndTimeBackward(DueTime endTime)
        {
            if (_productionOrder.IsReadOnly)
            {
                throw new MrpRunException("A readOnly entity cannot be changed anymore.");
            }
            _productionOrder.DueTime = endTime.GetValue();
        }

        public override void ClearStartTimeBackward()
        {
            if (_productionOrder.IsReadOnly)
            {
                throw new MrpRunException("A readOnly entity cannot be changed anymore.");
            }
            _productionOrder.DueTime = DueTime.INVALID_DUETIME;
        }

        public override void ClearEndTimeBackward()
        {
            if (_productionOrder.IsReadOnly)
            {
                throw new MrpRunException("A readOnly entity cannot be changed anymore.");
            }
            _productionOrder.DueTime = DueTime.INVALID_DUETIME;
        }

        public override State? GetState()
        {
            return null;
        }
    }
}