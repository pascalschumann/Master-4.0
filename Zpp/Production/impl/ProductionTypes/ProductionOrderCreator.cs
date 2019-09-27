using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.WrappersForCollections;

namespace Zpp.Mrp.ProductionManagement.ProductionTypes
{
    public abstract class ProductionOrderCreator: IProductionOrderCreator
    {
        public abstract EntityCollector CreateProductionOrder(Demand demand, Quantity quantity);
        
        
    }
}