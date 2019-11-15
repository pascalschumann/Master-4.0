using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.Util;

namespace Zpp.Mrp2.impl.Mrp1.impl.Production.impl.ProductionTypes
{
    /**
     * Here ProductionOrders.Count == given quantity productionOrders will be created
     */
    public class ProductionOrderCreatorAssemblyLine: ProductionOrderCreator
    {
        public ProductionOrderCreatorAssemblyLine()
        {
            if (ZppConfiguration.ProductionType.Equals(ProductionType.AssemblyLine) == false)
            {
                throw new MrpRunException("This is class is intended for productionType AssemblyLine.");
            }
        }

        public override EntityCollector CreateProductionOrder(Demand demand, Quantity quantity)
        {
            EntityCollector entityCollector = new EntityCollector();
            
            for (int i = 0; i < quantity.GetValue(); i++)
            {

                T_ProductionOrder tProductionOrder = new T_ProductionOrder();
                // [ArticleId],[Quantity],[Name],[DueTime],[ProviderId]
                tProductionOrder.DueTime = demand.GetStartTimeBackward().GetValue();
                tProductionOrder.Article = demand.GetArticle();
                tProductionOrder.ArticleId = demand.GetArticle().Id;
                tProductionOrder.Name = $"ProductionOrder for Demand {demand.GetArticle()}";
                tProductionOrder.Quantity = 1;

                ProductionOrder productionOrder =
                    new ProductionOrder(tProductionOrder);
                
                entityCollector.Add(productionOrder);
            }
            
            return entityCollector;
        }
    }
}