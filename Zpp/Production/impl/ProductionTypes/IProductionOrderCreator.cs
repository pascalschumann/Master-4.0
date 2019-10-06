using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain;
using Zpp.Mrp.impl;

namespace Zpp.Production.impl.ProductionTypes
{
    public interface IProductionOrderCreator
    {
        EntityCollector CreateProductionOrder(Demand demand, Quantity quantity);
    }
}