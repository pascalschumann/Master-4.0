using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.Util.StackSet;

namespace Zpp.Mrp2.impl.Scheduling.impl
{
    /**
     * The difference to DemandToProviderGraph is that no productionOrderBoms nodes are in it,
     * instead it has all operations of parent productionOrder.
     * Because scheduling(backward/forward/backward) must be done once for orders AND for operations.
     */
    public class OrderOperationGraph : DirectedGraph, IOrderOperationGraph
    {
        public OrderOperationGraph() : base()
        {
            // Don't try to remove subgraphs that rootType != customerOrderPart,
            // it's nearly impossible to correctly identify those (with performance in mind)
            
            // CreateGraph(dbTransactionData, aggregator);
            CreateGraph3();

            if (IsEmpty())
            {
                return;
            }
        }

        /**
         * No need to traverse --> graph is ready, just do some modifications:
         * remove ProductionOrderBoms, replace ProductionOrder by operationGraph
         */
        private void CreateGraph3()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();

            // remove ProductionOrderBoms
            foreach (var productionOrderBom in dbTransactionData.ProductionOrderBomGetAll())
            {
                var productionOrderBomNode = new Node(productionOrderBom);
                if (demandToProviderGraph.Contains(productionOrderBomNode))
                {
                    demandToProviderGraph.RemoveNode(productionOrderBomNode, true);
                }
            }

            // replace ProductionOrder by operationGraph
            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                if (productionOrder.IsReadOnly() == false)
                {
                    var productionOrderBomNode = new Node(productionOrder);
                    if (demandToProviderGraph.Contains(productionOrderBomNode))
                    {
                        OperationGraph operationGraph =
                            new OperationGraph((ProductionOrder) productionOrder);
                        demandToProviderGraph.ReplaceNodeByDirectedGraph(productionOrderBomNode, operationGraph);
                    }
                }
            }
            AddNodes(demandToProviderGraph.GetNodes());
        }

        /**
         * traverse top-down and remove ProductionOrderBom, replace ProductionOrder by operationGraph
         */
        private void CreateGraph2()
        {
            IStackSet<INode> visitedProductionOrders = new StackSet<INode>();
            foreach (var rootNode in GetRootNodes())
            {
                TraverseDemandToProviderGraph(rootNode, visitedProductionOrders);
            }
        }

        private void TraverseDemandToProviderGraph(INode node,
            IStackSet<INode> visitedProductionOrders)
        {
            if (node.GetEntity().GetType() == typeof(ProductionOrderBom))
            {
                // remove, ProductionOrderBoms will be ignored and replaced by operations
                RemoveNode(node, true);
            }
            else if (node.GetEntity().GetType() == typeof(ProductionOrder) &&
                     visitedProductionOrders.Contains(node) == false)
            {
                // insert it like it is in ProductionOrderToOperationGraph

                OperationGraph operationGraph =
                    new OperationGraph((ProductionOrder) node.GetEntity());
                ReplaceNodeByDirectedGraph(node, operationGraph);
                visitedProductionOrders.Push(node);
            }

            INodes successorNodes = GetSuccessorNodes(node);
            if (successorNodes == null)
            {
                return;
            }

            foreach (var successor in successorNodes)
            {
                TraverseDemandToProviderGraph(successor, visitedProductionOrders);
            }
        }
    }
}