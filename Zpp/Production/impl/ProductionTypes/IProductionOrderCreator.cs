using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;

namespace Zpp.Mrp.ProductionManagement.ProductionTypes
{
    public interface IProductionOrderCreator
    {
        EntityCollector CreateProductionOrder(Demand demand, Quantity quantity);

        Demands CreateDependingDemands(M_Article article, Provider parentProvider,
            Quantity demandedQuantity);
    }
}