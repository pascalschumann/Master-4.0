using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Common.DemandDomain;
using Zpp.DbCache;
using Zpp.Mrp.ProductionManagement;
using Zpp.Mrp.PurchaseManagement;
using Zpp.Production;

namespace Zpp.Mrp
{
    /**
     * abstracts over PurchaseManager+ProductionManager
     */
    public class OrderManager : IOrderManager
    {
        private readonly IPurchaseManager _purchaseManager;
        private readonly IProductionManager _productionManager;

        public OrderManager()
        {
            _purchaseManager = new PurchaseManager();
            _productionManager = new ProductionManager();
        }

        public EntityCollector Satisfy(Demand demand, Quantity demandedQuantity)
        {
            if (demand.GetArticle().ToBuild)
            {
                return _productionManager.Satisfy(demand,
                    demandedQuantity);
            }
            else
            {
                return _purchaseManager.Satisfy(demand,
                    demandedQuantity);
            }
        }
    }
}