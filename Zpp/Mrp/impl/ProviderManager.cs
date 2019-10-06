using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain.WrappersForCollections;
using Zpp.Production.impl;
using Zpp.Purchase.impl;
using Zpp.Stock.impl;

namespace Zpp.Mrp.impl
{
    /**
     * abstracts over PurchaseManager+ProductionManager+Stockmanager
     */
    public class ProviderManager : IProviderManager
    {
        private readonly PurchaseManager _purchaseManager;
        private readonly ProductionManager _productionManager;
        private readonly StockManager _stockManager;

        public ProviderManager()
        {
            _purchaseManager = new PurchaseManager();
            _productionManager = new ProductionManager();
            _stockManager = new StockManager();
        }

        public EntityCollector Satisfy(Demand demand, Quantity demandedQuantity)
        {
            // SE:I --> satisfy by orders (PuOP/PrOBom)
            if (demand.GetType() == typeof(StockExchangeDemand))
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
            // COP or PrOB --> satisfy by SE:W
            else
            {
                return _stockManager.Satisfy(demand, demandedQuantity);
            }
            
        }

        public EntityCollector CreateDependingDemands(Providers providers)
        {
            EntityCollector entityCollector = new EntityCollector();
            
            foreach (var provider in providers)
            {
                EntityCollector response;
                if (provider.GetType() == typeof(ProductionOrder))
                {
                    response = _productionManager.CreateDependingDemands(provider);
                    entityCollector.AddAll(response);
                }
                else if (provider.GetType() == typeof(StockExchangeProvider))
                {
                    response = _stockManager.CreateDependingDemands(provider);
                    entityCollector.AddAll(response);
                }
                
            }

            return entityCollector;
        }

        public void AdaptStock(Providers providers)
        {
            _stockManager.AdaptStock(providers);
        }
    }
}