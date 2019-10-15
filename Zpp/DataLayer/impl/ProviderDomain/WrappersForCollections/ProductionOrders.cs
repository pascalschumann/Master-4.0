using System.Collections.Generic;
using Master40.DB.DataModel;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;

namespace Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections
{
    /**
     * wraps the collection with all productionOrders
     */
    public class ProductionOrders : Providers
    {
        public ProductionOrders(List<Provider> providers) : base(providers)
        {
        }

        public ProductionOrders()
        {
        }

        public ProductionOrders(Provider provider) : base(provider)
        {
        }
        
        public ProductionOrders(List<T_ProductionOrder> iDemands) : base(ToProviders(iDemands))
        {
        }

        private static List<Provider> ToProviders(List<T_ProductionOrder> iProviders)
        {
            List<Provider> providers = new List<Provider>();
            foreach (var iProvider in iProviders)
            {
                providers.Add(new ProductionOrder(iProvider));
            }

            return providers;
        }

    }
}