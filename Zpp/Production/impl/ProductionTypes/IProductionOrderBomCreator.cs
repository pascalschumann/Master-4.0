using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.ProviderDomain.Wrappers;

namespace Zpp.Production.impl.ProductionTypes
{
    public interface IProductionOrderBomCreator
    {
        Demands CreateProductionOrderBomsForArticleBom(
            M_ArticleBom articleBom, Quantity quantity,
            ProductionOrder parentProductionOrder);
    }
}