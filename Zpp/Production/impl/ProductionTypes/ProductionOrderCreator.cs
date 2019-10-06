using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain;
using Zpp.Mrp.impl;

namespace Zpp.Production.impl.ProductionTypes
{
    public abstract class ProductionOrderCreator: IProductionOrderCreator
    {
        public abstract EntityCollector CreateProductionOrder(Demand demand, Quantity quantity);
        
        
    }
}