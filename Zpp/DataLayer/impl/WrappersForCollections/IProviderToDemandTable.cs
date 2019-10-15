using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer.impl.ProviderDomain;

namespace Zpp.DataLayer.impl.WrappersForCollections
{
    public interface IProviderToDemandTable: ICollectionWrapper<T_ProviderToDemand>
    {
        void Add(Provider provider, Id demandId, Quantity quantity);
    }
}