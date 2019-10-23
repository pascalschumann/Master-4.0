using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
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

namespace Zpp.Mrp2.impl.Scheduling.impl
{
    /**
     * The difference to DemandToProviderGraph is that no productionOrderBoms nodes are in it,
     * instead it has all operations of parent productionOrder.
     * Because scheduling must be done once for orders AND for operations.
     */
    public class OrderOperationGraph : DirectedGraph, IOrderOperationGraph
    {
        public OrderOperationGraph() : base()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            CreateGraph(dbTransactionData, aggregator);
            
            // remove subgraphs that has roots != customerOrderPart
            foreach (var root in GetRootNodes())
            {
                if (root.GetEntity().GetType() != typeof(CustomerOrderPart))
                {
                    RemoveTopDown(root);
                }
            }
        }

        private void CreateGraph(IDbTransactionData dbTransactionData, IAggregator aggregator)
        {
            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(new Id(demandToProvider.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(demandToProvider.ProviderId));
                if (demand == null || provider == null)
                {
                    throw new MrpRunException("Demand/Provider should not be null.");
                }

                if (demand.GetType() == typeof(ProductionOrderBom))
                {
                    // pass, no action here, ProductionOrderBoms will be ignored and replaced by operations
                }
                else
                {
                    INode fromNode = new Node(demand);
                    INode toNode = new Node(provider);
                    AddEdge(new Edge(demandToProvider, fromNode, toNode));
                }
            }

            foreach (var providerToDemand in dbTransactionData.ProviderToDemandGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(new Id(providerToDemand.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(providerToDemand.ProviderId));
                if (demand == null || provider == null)
                {
                    throw new MrpRunException("Demand/Provider should not be null.");
                }


                if (provider.GetType() == typeof(ProductionOrder))
                {
                    // insert it like it is in ProductionOrderToOperationGraph
                    
                    List<ProductionOrderOperation> productionOrderOperations =
                        aggregator.GetProductionOrderOperationsOfProductionOrder(
                            providerToDemand.GetProviderId());

                    INode productionOrderNode =
                        new Node(provider);

                    IDirectedGraph<INode> productionOrderOperationGraph =
                        new ProductionOrderOperationGraph(
                            (ProductionOrder) productionOrderNode.GetEntity());
                    if (productionOrderOperations.Count.Equals(productionOrderOperationGraph
                            .GetAllHeadNodes().Count()) == false)
                    {
                        throw new MrpRunException(
                            "One of the compared collections do not have all operations.");
                    }

                    AddEdges(productionOrderOperationGraph.GetEdges());
                    // connect
                    foreach (var operation in productionOrderOperations)
                    {
                        ProductionOrderBoms productionOrderBoms =
                            aggregator.GetAllProductionOrderBomsBy(operation);
                        foreach (var productionOrderBom in productionOrderBoms)
                        {
                            IProviders childProviders =
                                aggregator.GetAllChildProvidersOf(productionOrderBom);
                            if (childProviders.Count() != 1)
                            {
                                throw new MrpRunException(
                                    "Every ProductionOrderBom must have exact one provider.");
                            }

                            Provider childProvider = childProviders.GetAll()[0];
                            AddEdge(new Edge(new Node(operation),
                                new Node(childProvider)));
                        }
                    }
                }
                else
                {
                    INode fromNode = new Node(provider);
                    INode toNode = new Node(demand);
                    AddEdge(new Edge(providerToDemand, fromNode, toNode));
                }
            }
        }
    }
}