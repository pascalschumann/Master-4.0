using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.WrapperForEntities;

namespace Zpp.Mrp2.impl.Mrp1.impl.Production.impl.ProductionTypes
{
    public abstract class ProductionOrderCreator: IProductionOrderCreator
    {
        public abstract EntityCollector CreateProductionOrder(Demand demand, Quantity quantity);
        
        
    }
}