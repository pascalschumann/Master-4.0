using System.Collections.Generic;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;

namespace Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections
{
    /**
     * wraps the collection with all stockExchangeProviders
     */
    public class StockExchangeProviders : Providers
    {
        public StockExchangeProviders(List<T_StockExchange> iDemands) : base(ToProviders(iDemands))
        {
        }

        private static List<Provider> ToProviders(List<T_StockExchange> iProviders)
        {
            List<Provider> providers = new List<Provider>();
            foreach (var iProvider in iProviders)
            {
                if (iProvider.StockExchangeType.Equals(StockExchangeType.Demand))
                {
                    continue;
                }
                providers.Add(new StockExchangeProvider(iProvider));
            }

            return providers;
        }
    }
}