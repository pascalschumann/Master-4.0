using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.WrapperForEntities;

namespace Zpp.DataLayer
{
    public interface IOpenDemandManager
    {
        void AddDemand(Demand oneDemand, Quantity reservedQuantity);

        EntityCollector SatisfyProviderByOpenDemand(Provider provider, Quantity demandedQuantity);
        
        void Dispose();
    }
}