using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.Util;

namespace Zpp.Mrp2.impl.Mrp1.impl.Production.impl.ProductionTypes
{
    /**
     * Here one ProductionOrder with productionOrder.Quantity == given quantity will be created
     */
    public class ProductionOrderCreatorWorkshopClassic : ProductionOrderCreator
    {
        public ProductionOrderCreatorWorkshopClassic()
        {
            if (ZppConfiguration.ProductionType.Equals(ProductionType.WorkshopProductionClassic) == false)
            {
                throw new MrpRunException(
                    "This is class is intended for productionType WorkshopProductionClassic.");
            }
        }

        public override EntityCollector CreateProductionOrder(Demand demand, Quantity quantity)
        {
            T_ProductionOrder tProductionOrder = new T_ProductionOrder();
            // [ArticleId],[Quantity],[Name],[DueTime],[ProviderId]
            tProductionOrder.DueTime = demand.GetStartTime().GetValue();
            tProductionOrder.Article = demand.GetArticle();
            tProductionOrder.ArticleId = demand.GetArticle().Id;
            tProductionOrder.Name = $"ProductionOrder for Demand {demand.GetArticle()}";
            tProductionOrder.Quantity = quantity.GetValue();

            ProductionOrder productionOrder =
                new ProductionOrder(tProductionOrder);

            EntityCollector entityCollector = new EntityCollector();
            entityCollector.Add(productionOrder);
            
            return entityCollector;
        }
    }
}