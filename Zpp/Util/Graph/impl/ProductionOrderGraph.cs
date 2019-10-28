using System.Collections.Generic;
using System.Linq;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph.impl
{
    public class ProductionOrderGraph : DemandToProviderGraph
    {
        public ProductionOrderGraph() : base()
        {
            if (IsEmpty())
            {
                return;
            }

            // CreateGraphOld();

            CreateGraph();
        }
        
        private void CreateGraph()
        {
            List<IEdge> edges = new List<IEdge>();
            foreach (var rootNode in GetRootNodes())
            {
                Traverse( rootNode,  null,  edges);
            }
            Edges = new StackSet<IEdge>();
            Edges.PushAll(edges);
        }

        private void Traverse(INode node, INode lastProductionOrder, List<IEdge> edges)
        {
            if (node.GetEntity().GetType() == typeof(ProductionOrder))
            {
                if (lastProductionOrder != null)
                {
                    // connect
                    edges.Add(new Edge(lastProductionOrder, node));
                }
                lastProductionOrder = node;
            }
            
            
            INodes successorNodes = GetSuccessorNodes(node);
            if (successorNodes != null)
            {
                foreach (var successorNode in successorNodes)
                {
                    Traverse(successorNode, lastProductionOrder,  edges);
                }
            }
        }

        private void CreateGraphOld( )
        {
            foreach (var uniqueNode in GetAllUniqueNodes())
            {
                if (uniqueNode.GetEntity().GetType() != typeof(ProductionOrder)
                    // && uniqueNode.GetEntity().GetType() != typeof(ProductionOrderBom)
                )
                {
                    RemoveNode(uniqueNode);
                }
                /*else
                {
                    if (includeProductionOrdersWithoutOperations == false)
                    {
                        ProductionOrder productionOrder = (ProductionOrder) uniqueNode.GetEntity();
                        if (ZppConfiguration.CacheManager.GetAggregator()
                                .GetProductionOrderOperationsOfProductionOrder(productionOrder) ==
                            null)
                        {
                            RemoveNode(uniqueNode);
                        }
                    }
                }*/
            }
        }
    }
}