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
            _productionOrderGraph = new ProductionOrderGraph(false);

            foreach (var productionOrderNode in _productionOrderGraph.GetAllUniqueNodes())
            {
                IDirectedGraph<INode> productionOrderOperationGraph =
                    new ProductionOrderOperationGraph((ProductionOrder) productionOrderNode.GetEntity());
                
                // connect
                _productionOrderGraph.ReplaceNodeByDirectedGraph(productionOrderNode,
                    productionOrderOperationGraph);
            }

            Edges = _productionOrderGraph.GetEdges();
            
            // _productionOrderGraph = new ProductionOrderDirectedGraph(_dbTransactionData, false);
        }

        public ProductionOrderOperations GetAllOperations()
        {
            ProductionOrderOperations productionOrderOperations = new ProductionOrderOperations();
            foreach (var uniqueNode in GetAllUniqueNodes())
            {
                IScheduleNode entity = uniqueNode.GetEntity();
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
                    IScheduleNode entity = uniqueNode.GetEntity();
                    if (entity.GetType() == typeof(ProductionOrder))
                    {
                        productionOrders.Add(((ProductionOrder) entity));
                    }
                }

            return productionOrders;
        }

        public void GetPredecessorOperations(IStackSet<ProductionOrderOperation> predecessorOperations, INode node)
        {
            INodes predecessors = GetPredecessorNodes(node);
            if (predecessors == null)
            {
                return;
            }
            foreach (var predecessor in predecessors)
            {
                IScheduleNode entity = predecessor.GetEntity();
                if (entity.GetType() == typeof(ProductionOrderOperation))
                {
                    predecessorOperations.Push((ProductionOrderOperation)entity);
                }
                else
                {
                    GetPredecessorOperations(predecessorOperations, predecessor);
                }
            }
        }

        public void GetLeafOperations(IStackSet<ProductionOrderOperation> leafOperations)
        {
            INodes leafs = GetLeafNodes();

            if (leafs == null)
            {
                return;
            }
            foreach (var leaf in leafs)
            {
                IScheduleNode entity = leaf.GetEntity();
                if (entity.GetType() == typeof(ProductionOrderOperation))
                {
                    leafOperations.Push((ProductionOrderOperation)entity);
                }
                else
                {
                    GetPredecessorOperations(leafOperations, leaf);
                }
            }
        }
    }
}