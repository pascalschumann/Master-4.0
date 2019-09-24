using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Common.DemandDomain;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;

namespace Zpp.Mrp.ProductionManagement.ProductionTypes
{
    public interface IProductionOrderCreator
    {
        ProductionOrders CreateProductionOrder(
            Demand demand, Quantity quantity);
    }
}