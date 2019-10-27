using System.Collections.Generic;

namespace Zpp.DataLayer.impl.WrappersForCollections
{
    public class DemandOrProviders: CollectionWrapperWithStackSet<IDemandOrProvider>
    {
        public DemandOrProviders()
        {
        }

        public DemandOrProviders(IEnumerable<IDemandOrProvider> list) : base(list)
        {
        }

        public DemandOrProviders(IDemandOrProvider item) : base(item)
        {
        }
    }
}