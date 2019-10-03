using System.Collections.Generic;
using Zpp.DataLayer;

namespace Zpp.WrappersForCollections
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