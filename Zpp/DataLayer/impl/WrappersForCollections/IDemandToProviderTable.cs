using Master40.DB.DataModel;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.ProviderDomain;

namespace Zpp.DataLayer.impl.WrappersForCollections
{
    public interface IDemandToProviderTable: ICollectionWrapper<T_DemandToProvider>
    {
        bool Contains(Demand demand);
        
        bool Contains(Provider provider);
    }
}