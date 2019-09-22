using System.Collections.Generic;
using Master40.DB.DataModel;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.DbCache;
using Zpp.WrappersForCollections;

namespace Zpp.Common.ProviderDomain.WrappersForCollections
{
    public class ProductionOrderOperations : CollectionWrapperWithList<ProductionOrderOperation>
    {
        public ProductionOrderOperations(IEnumerable<T_ProductionOrderOperation> list
            ) : base(Wrap(list))
        {
        }

        public ProductionOrderOperations()
        {
        }

        private static List<ProductionOrderOperation> Wrap(
            IEnumerable<T_ProductionOrderOperation> list)
        {
            List<ProductionOrderOperation> productionOrderOperations =
                new List<ProductionOrderOperation>();
            foreach (var item in list)
            {
                productionOrderOperations.Add(new ProductionOrderOperation(item));
            }

            return productionOrderOperations;
        }
        
        public List<T_ProductionOrderOperation> GetAllAsT_ProductionOrderOperation()
        {
            List<T_ProductionOrderOperation> providers = new List<T_ProductionOrderOperation>();
            foreach (var operation in List)
            {
                providers.Add( operation.GetValue());
            }

            return providers;
        }
    }
}