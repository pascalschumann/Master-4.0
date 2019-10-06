using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.ProviderDomain.WrappersForCollections;
using Zpp.Mrp.impl;

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