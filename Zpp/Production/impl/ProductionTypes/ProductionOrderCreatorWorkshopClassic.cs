using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.Utils;

namespace Zpp.Mrp.ProductionManagement.ProductionTypes
{
    /**
     * Here one ProductionOrder with productionOrder.Quantity == given quantity will be created
     */
    public class ProductionOrderCreatorWorkshopClassic : ProductionOrderCreator
    {
        public ProductionOrderCreatorWorkshopClassic()
        {
            if (Configuration.ZppConfiguration.ProductionType.Equals(ProductionType.WorkshopProductionClassic) == false)
            {
                throw new MrpRunException(
                    "This is class is intended for productionType WorkshopProductionClassic.");
            }
        }

        public override EntityCollector CreateProductionOrder(Demand demand, Quantity quantity)
        {
            T_ProductionOrder tProductionOrder = new T_ProductionOrder();
            // [ArticleId],[Quantity],[Name],[DueTime],[ProviderId]
            tProductionOrder.DueTime = demand.GetDueTime().GetValue();
            tProductionOrder.Article = demand.GetArticle();
            tProductionOrder.ArticleId = demand.GetArticle().Id;
            tProductionOrder.Name = $"ProductionOrder for Demand {demand.GetArticle()}";
            tProductionOrder.Quantity = quantity.GetValue();

            ProductionOrder productionOrder =
                new ProductionOrder(tProductionOrder);

            Demands dependingDemands = CreateDependingDemands(demand.GetArticle(), productionOrder, productionOrder.GetQuantity());
            
            EntityCollector entityCollector = new EntityCollector();
            entityCollector.AddAll(dependingDemands);
            entityCollector.Add(productionOrder);
            
            return entityCollector;
        }
    }
}