using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;

namespace Zpp.Util.Graph.impl
{
    public class OperationGraph : DirectedGraph
    {
        public OperationGraph(ProductionOrder productionOrder) : base()
        {
            CreateGraph2(productionOrder);
        }

        private void CreateGraph2(ProductionOrder productionOrder)
        {
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();
            IEnumerable<ProductionOrderOperation> productionOrderOperations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            if (productionOrderOperations == null)
            {
                Clear();
                return;
            }

            productionOrderOperations =
                productionOrderOperations.OrderBy(x => x.GetHierarchyNumber().GetValue());
            ProductionOrderOperation predecessor = null;
            foreach (var operation in productionOrderOperations)
            {
                if (predecessor == null)
                {
                    predecessor = operation;
                }
                else
                {
                    AddEdge(new Edge(new Node(predecessor), new Node(operation)));
                }
            }
        }

        /**
         * hieraricalc version with parallel operations possible, way to complex, it's a list ordered by hierarchy number
         */
        private void CreateGraph1(ProductionOrder productionOrder)
        {
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            Dictionary<HierarchyNumber, List<ProductionOrderOperation>>
                hierarchyToProductionOrderOperation =
                    new Dictionary<HierarchyNumber, List<ProductionOrderOperation>>();

            IDirectedGraph<INode> directedGraph = new DirectedGraph();

            List<ProductionOrderOperation> productionOrderOperations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            if (productionOrderOperations == null)
            {
                Clear();
                return;
            }

            foreach (var productionOrderOperation in productionOrderOperations)
            {
                HierarchyNumber hierarchyNumber = productionOrderOperation.GetHierarchyNumber();
                if (hierarchyToProductionOrderOperation.ContainsKey(hierarchyNumber) == false)
                {
                    hierarchyToProductionOrderOperation.Add(hierarchyNumber,
                        new List<ProductionOrderOperation>());
                }

                hierarchyToProductionOrderOperation[hierarchyNumber].Add(productionOrderOperation);
            }

            List<HierarchyNumber> hierarchyNumbers = hierarchyToProductionOrderOperation.Keys
                .OrderByDescending(x => x.GetValue()).ToList();
            int i = 0;
            foreach (var hierarchyNumber in new HashSet<HierarchyNumber>(hierarchyNumbers))
            {
                foreach (var productionOrderOperation in hierarchyToProductionOrderOperation[
                    hierarchyNumber])
                {
                    if (i.Equals(0))
                    {
                        directedGraph.AddEdge(new Edge(new Node(productionOrder),
                            new Node(productionOrderOperation)));
                    }
                    else
                    {
                        foreach (var productionOrderOperationBefore in
                            hierarchyToProductionOrderOperation[hierarchyNumbers[i - 1]])
                        {
                            directedGraph.AddEdge(new Edge(new Node(productionOrderOperationBefore),
                                new Node(productionOrderOperation)));
                        }
                    }
                }

                i++;
            }

            Clear();
            AddNodes(directedGraph.GetNodes());
        }
    }
}