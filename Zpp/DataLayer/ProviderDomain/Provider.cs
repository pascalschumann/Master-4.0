using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DbCache;
using Zpp.OrderGraph;
using Zpp.Utils;
using Zpp.WrappersForCollections;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.ProviderDomain
{
    /**
     * Provides default implementations for interface methods, can be moved to interface once C# 8.0 is released
     */
    public abstract class Provider : IProviderLogic, INode, IDemandOrProvider
    {
        protected readonly IProvider _provider;
        protected readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public Provider(IProvider provider)
        {
            if (provider == null)
            {
                throw new MrpRunException("Given provider should not be null.");
            }
            _provider = provider;
            
        }

        public abstract DueTime GetDueTime();

        public abstract IProvider ToIProvider();
        
        public override bool Equals(object obj)
        {
            var item = obj as Provider;

            if (item == null)
            {
                return false;
            }

            return _provider.Id.Equals(item._provider.Id);
        }
        
        public override int GetHashCode()
        {
            return _provider.Id.GetHashCode();
        }

        public Quantity GetQuantity()
        {
            return _provider.GetQuantity();
        }

        public abstract Id GetArticleId();

        public bool ProvidesMoreThan(Quantity quantity)
        {
            return GetQuantity().IsGreaterThanOrEqualTo(quantity);
        }
        
        /**
         * returns Quantity.Null or higher
         */
        public static Quantity CalcQuantityProvidedByProvider(Quantity providedQuantity,
            Quantity demandedQuantity)
        {
            if (!providedQuantity.IsGreaterThan(Quantity.Null()))
            {
                return Quantity.Null();
            }

            if (providedQuantity.IsGreaterThanOrEqualTo(demandedQuantity))
            {
                return demandedQuantity;
            }

            return providedQuantity;
        }

        public M_Article GetArticle()
        {
            return _dbMasterDataCache.M_ArticleGetById(GetArticleId());
        }

        public Id GetId()
        {
            return new Id(_provider.Id);
        }

        public override string ToString()
        {
            return $"{GetId()}: {GetArticle().Name};{GetQuantity()}";
        }

        public NodeType GetNodeType()
        {
            return NodeType.Provider;
        }

        public INode GetEntity()
        {
            return this;
        }

        public DueTime GetStartTime()
        {
            return GetEndTime().Minus(new DueTime(GetDuration()));
        }

        public abstract void SetProvided(DueTime atTime);

        public abstract Duration GetDuration();

        public abstract DueTime GetEndTime();

        public abstract void SetStartTime(DueTime startTime);
        
        public static Provider AsProvider(IDemandOrProvider demandOrProvider)
        {
           
            if (demandOrProvider.GetType() == typeof(ProductionOrder))
            {
                return (ProductionOrder)demandOrProvider;
            }
            else if (demandOrProvider.GetType() == typeof(PurchaseOrderPart))
            {
                return (PurchaseOrderPart)demandOrProvider;
            }
            else if (demandOrProvider.GetType() == typeof(StockExchangeProvider))
            {
                return (StockExchangeProvider)demandOrProvider;
            }
            else
            {
                throw new MrpRunException("Unknown type implementing Provider");
            }
        }

        public abstract void SetDone();

        public abstract void SetInProgress();

        public abstract bool IsDone();
    }
}