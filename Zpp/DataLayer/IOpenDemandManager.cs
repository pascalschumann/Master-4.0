using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.ProviderDomain;
using Zpp.Mrp.impl;

namespace Zpp.DataLayer
{
    public interface IOpenDemandManager
    {
        void AddDemand(Demand oneDemand, Quantity reservedQuantity);

        EntityCollector SatisfyProviderByOpenDemand(Provider provider, Quantity demandedQuantity);
        
        void Dispose();
    }
}