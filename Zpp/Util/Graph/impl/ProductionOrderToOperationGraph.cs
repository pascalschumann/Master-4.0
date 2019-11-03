using System.Linq;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph.impl
{
    /**
     * Replaces each productionOrder in productionorderGraph with it's operationgraph
     * without the productionOrder --> graph contains only operations
     */
    public class ProductionOrderToOperationGraph : DirectedGraph
    {
        private readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();


        public ProductionOrderToOperationGraph() : base()
        {
            IDirectedGraph<INode> productionOrderGraph = new ProductionOrderGraph();
            if (productionOrderGraph.IsEmpty())
            {
                throw new MrpRunException("How could it happen, that the _productionOrderGraph is empty ?");
            }

            foreach (var productionOrderNode in productionOrderGraph.GetAllUniqueNodes())
            {
                IDirectedGraph<INode> operationGraph =
                    new OperationGraph((ProductionOrder) productionOrderNode.GetEntity());
                
                // connect
                productionOrderGraph.ReplaceNodeByDirectedGraph(productionOrderNode,
                    operationGraph);
                // we don't need the productionOrder as root
                productionOrderGraph.RemoveNode(productionOrderNode, true);
            }
            
            _nodes = productionOrderGraph.GetNodes();

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

    }
}