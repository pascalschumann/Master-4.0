using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.ProviderDomain;
using Zpp.DataLayer.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.WrappersForCollections;

namespace Zpp.DataLayer.impl.OpenDemand
{
    /**
    * Maintains created providers
    */
    public interface IProviderManager
    {
        /**
         * @returns: (demandedQuantity - provider.getQuantity()), Quantity.Null if provider.getQuantity() is bigger than demandedQuantity
         */
        void AddProvider(Id demandId, Provider provider, Quantity reservedQuantity);

        /**
         * sum(quantity) over given demandId
         * aka select count(Quantity) where DemandId=demandId
         */
        Quantity GetSatisfiedQuantityOfDemand(Id demandId);

        bool IsSatisfied(Demand demand);

        /**
         * sum(quantity) over given providerId
         * aka select count(Quantity) where ProviderId=providerId
         */
        Quantity GetReservedQuantityOfProvider(Id providerId);

        /**
         * aka depending demands of new added providers.
         * NOTE: you can only call it once, then it will be cleared internally.
         */
        Demands GetNextDemands();

        IDemandToProviderTable GetDemandToProviderTable();

        IProviderToDemandTable GetProviderToDemandTable();

        IProviders GetProviders();

        void AddDemandToProvider(T_DemandToProvider demandToProvider);
     
    }
}