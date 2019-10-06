using System.Collections.Generic;
using System.Linq;
using Master40.DB.DataModel;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.ProviderDomain;

namespace Zpp.DataLayer.WrappersForCollections
{
    /**
     * wraps T_DemandToProvider
     */
    public class DemandToProviderTable : CollectionWrapperWithStackSet<T_DemandToProvider>, IDemandToProviderTable
    {
        public DemandToProviderTable(List<T_DemandToProvider> list) : base(list)
        {
        }

        public DemandToProviderTable()
        {
        }

        public bool Contains(Demand demand)
        {
            return StackSet.Select(x => x.DemandId).ToList()
                .Contains(demand.GetId().GetValue());
        }

        public bool Contains(Provider provider)
        {
            return StackSet.Select(x => x.ProviderId).ToList()
                .Contains(provider.GetId().GetValue());
        }

    }
}