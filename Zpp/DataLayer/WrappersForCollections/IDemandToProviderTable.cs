using Master40.DB.DataModel;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.ProviderDomain;

namespace Zpp.DataLayer.WrappersForCollections
{
    public interface IDemandToProviderTable: ICollectionWrapper<T_DemandToProvider>
    {
        bool Contains(Demand demand);
        
        bool Contains(Provider provider);
    }
}