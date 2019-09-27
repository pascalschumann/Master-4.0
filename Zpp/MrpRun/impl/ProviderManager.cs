using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Common.DemandDomain;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.Mrp.NodeManagement;
using Zpp.Mrp.ProductionManagement;
using Zpp.Mrp.PurchaseManagement;
using Zpp.Mrp.StockManagement;
using Zpp.Production;

namespace Zpp.Mrp
{
    /**
     * abstracts over PurchaseManager+ProductionManager+Stockmanager
     */
    public class ProviderManager : IProviderManager
    {
        private readonly IPurchaseManager _purchaseManager;
        private readonly IProductionManager _productionManager;
        private readonly IStockManager _stockManager;

        public ProviderManager()
        {
            _purchaseManager = new PurchaseManager();
            _productionManager = new ProductionManager();
            _stockManager = new StockManager();
        }

        public EntityCollector Satisfy(Demand demand, Quantity demandedQuantity)
        {
            if (demand.GetArticle().ToBuild)
            {
                return _productionManager.Satisfy(demand, demandedQuantity);
            }
            else
            {
                return _purchaseManager.Satisfy(demand, demandedQuantity);
            }
        }

        public EntityCollector CreateDependingDemands(Provider provider)
        {
            if (provider.GetType() == typeof(ProductionOrder))
            {
                return _productionManager.CreateDependingDemands(provider);
            }
            else
            {
                return _stockManager.CreateDependingDemands(provider);
            }
        }

        public void AdaptStock(Providers providers)
        {
            _stockManager.AdaptStock(providers);
        }
    }
}