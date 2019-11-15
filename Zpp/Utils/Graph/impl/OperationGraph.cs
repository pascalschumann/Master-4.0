using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph.impl
{
    /**
     * root is production order, followed by operations ordered by hierarchyNumber
     */
    public class OperationGraph : DirectedGraph
    {
        public OperationGraph(ProductionOrder productionOrder) : base()
        {
            CreateGraph2(productionOrder);
        }

        private void CreateGraph2(ProductionOrder productionOrder)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IEnumerable<ProductionOrderOperation> productionOrderOperations = dbTransactionData
                .ProductionOrderOperationGetAll().GetAll().Where(x =>
                    x.GetValue().ProductionOrderId.Equals(productionOrder.GetId().GetValue()))
                .OrderBy(x => x.GetHierarchyNumber().GetValue());
            ;
            if (productionOrderOperations.Any() == false)
            {
                Clear();
                return;
            }

            // root is always the productionOrder
            INode predecessor = new Node(productionOrder);
            foreach (var operation in productionOrderOperations)
            {
                INode operationNode = new Node(operation);
                AddEdge(new Edge(predecessor, operationNode));
                predecessor = operationNode;
            }
        }
    }
}