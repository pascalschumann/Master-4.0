using Zpp.Configuration;
using Zpp.DataLayer.ProviderDomain.Wrappers;

namespace Zpp.Util.Graph.impl
{
    public class ProductionOrderDirectedGraph : DemandToProviderDirectedGraph, IDirectedGraph<INode>
    {
        public ProductionOrderDirectedGraph(bool includeProductionOrdersWithoutOperations) : base()
        {

            foreach (var uniqueNode in GetAllUniqueNodes())
            {
                if (uniqueNode.GetEntity().GetType() != typeof(ProductionOrder)
                    // && uniqueNode.GetEntity().GetType() != typeof(ProductionOrderBom)
                )
                {
                    RemoveNode(uniqueNode);
                }
                else
                {
                    if (includeProductionOrdersWithoutOperations == false)
                    {
                        ProductionOrder productionOrder = (ProductionOrder) uniqueNode.GetEntity();
                        if (ZppConfiguration.CacheManager.GetAggregator()
                            .GetProductionOrderOperationsOfProductionOrder(productionOrder) == null)
                        {
                            RemoveNode(uniqueNode);        
                        }
                    }
                }

                /*if (uniqueNode.GetEntity().GetType() == typeof(ProductionOrderBom))
                {
                    ProductionOrderBom productionOrderBom = (ProductionOrderBom)uniqueNode.GetEntity();
                    if (productionOrderBom.HasOperation() == false)
                    {
                        RemoveNode(uniqueNode);
                    }
                }*/
            }
        }
    }
}