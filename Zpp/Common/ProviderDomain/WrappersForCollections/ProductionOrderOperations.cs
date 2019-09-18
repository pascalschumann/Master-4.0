using System.Collections.Generic;
using Master40.DB.DataModel;
using Zpp.WrappersForCollections;

namespace Zpp.Common.ProviderDomain.WrappersForCollections
{
    public class ProductionOrderOperations: CollectionWrapperWithList<T_ProductionOrderOperation>
    {
        public ProductionOrderOperations(IEnumerable<T_ProductionOrderOperation> list) : base(list)
        {
        }

        public ProductionOrderOperations()
        {
        }
    }
}