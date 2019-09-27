using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Common.DemandDomain;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.Mrp.NodeManagement;

namespace Zpp.Mrp
{
    /**
     * Central interface for the mainModules
     */
    public interface IProviderManager
    {
        EntityCollector Satisfy(Demand demand, Quantity demandedQuantity);

        EntityCollector CreateDependingDemands(Providers providers);
        
        void AdaptStock(Providers providers);
    }
}