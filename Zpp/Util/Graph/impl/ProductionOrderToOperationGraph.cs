using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain.WrappersForCollections;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph.impl
{
    public class ProductionOrderToOperationGraph : DirectedGraph, IProductionOrderToOperationGraph<INode>
    {
        private readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
        private readonly IDirectedGraph<INode> _productionOrderGraph;


        public ProductionOrderToOperationGraph() : base()
        {
            _productionOrderGraph = new ProductionOrderDirectedGraph(false);

            foreach (var productionOrderNode in _productionOrderGraph.GetAllUniqueNodes())
            {
                IDirectedGraph<INode> productionOrderOperationGraph =
                    new ProductionOrderOperationDirectedGraph((ProductionOrder) productionOrderNode.GetEntity());
                
                // connect
                _productionOrderGraph.ReplaceNodeByDirectedGraph(productionOrderNode,
                    productionOrderOperationGraph);
            }

            _adjacencyList = _productionOrderGraph.GetAdjacencyList();
            
            // _productionOrderGraph = new ProductionOrderDirectedGraph(_dbTransactionData, false);
        }

        public ProductionOrderOperations GetAllOperations()
        {
            ProductionOrderOperations productionOrderOperations = new ProductionOrderOperations();
            foreach (var uniqueNode in GetAllUniqueNodes())
            {
                INode entity = uniqueNode.GetEntity();
                if (entity.GetType() == typeof(ProductionOrderOperation))
                {
                    productionOrderOperations.Add((ProductionOrderOperation) entity);
                }
            }

            return productionOrderOperations;
        }

        public ProductionOrders GetAllProductionOrders()
        {
            ProductionOrders productionOrders = new ProductionOrders();

                foreach (var uniqueNode in GetAllUniqueNodes())
                {
                    INode entity = uniqueNode.GetEntity();
                    if (entity.GetType() == typeof(ProductionOrder))
                    {
                        productionOrders.Add(((ProductionOrder) entity));
                    }
                }

            return productionOrders;
        }

        public void GetPredecessorOperations(IStackSet<INode> predecessorOperations, INode node)
        {
            INodes predecessors = GetPredecessorNodes(node);
            if (predecessors == null)
            {
                return;
            }
            foreach (var predecessor in predecessors)
            {
                INode entity = predecessor.GetEntity();
                if (entity.GetType() == typeof(ProductionOrderOperation))
                {
                    predecessorOperations.Push(entity);
                }
                else
                {
                    GetPredecessorOperations(predecessorOperations, predecessor);
                }
            }
        }

        public void GetLeafOperations(IStackSet<INode> leafOperations)
        {
            INodes leafs = GetLeafNodes();

            if (leafs == null)
            {
                return;
            }
            foreach (var leaf in leafs)
            {
                INode entity = leaf.GetEntity();
                if (entity.GetType() == typeof(ProductionOrderOperation))
                {
                    leafOperations.Push(entity);
                }
                else
                {
                    GetPredecessorOperations(leafOperations, leaf);
                }
            }
        }
    }
}