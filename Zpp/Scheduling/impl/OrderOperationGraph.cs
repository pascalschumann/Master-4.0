using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.ProviderDomain;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.Util.StackSet;

namespace Zpp.Scheduling.impl
{
    /**
     * The difference to DemandToProviderGraph is that no productionOrderBoms nodes are in it,
     * instead it has all operations of parent productionOrder.
     * Because scheduling must be done once for orders AND for operations.
     */
    public class OrderOperationGraph : DirectedGraph, IDirectedGraph<INode>
    {
        public OrderOperationGraph() : base()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(new Id(demandToProvider.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(demandToProvider.ProviderId));
                if (demand == null || provider == null)
                {
                    throw new MrpRunException("Demand/Provider should not be null.");
                }

                if (provider.GetType() == typeof(ProductionOrderBom))
                {
                    // pass, no action here, ProductionOrderBoms will be ignored and replaced by operations
                }
                else
                {
                    INode fromNode = new Node(demand, demandToProvider.GetDemandId());
                    INode toNode = new Node(provider, demandToProvider.GetProviderId());
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
                    List<ProductionOrderOperation> productionOrderOperations =
                        aggregator.GetProductionOrderOperationsOfProductionOrder(
                            providerToDemand.GetProviderId());

                    INode productionOrderNode =
                        new Node(provider, providerToDemand.GetProviderId());

                    IDirectedGraph<INode> productionOrderOperationGraph =
                        new ProductionOrderOperationGraph(
                            (ProductionOrder) productionOrderNode.GetEntity());
                    if (productionOrderOperations.Count.Equals(productionOrderOperationGraph
                            .GetAllHeadNodes().ToStackSet().Count()) == false)
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
                            AddEdge(new Edge(new Node(operation, operation.GetId()),
                                new Node(childProvider, childProvider.GetId())));
                        }
                    }
                }
                else
                {
                    INode fromNode = new Node(provider, providerToDemand.GetProviderId());
                    INode toNode = new Node(demand, providerToDemand.GetDemandId());
                    AddEdge(new Edge(providerToDemand, fromNode, toNode));
                }
            }
        }
    }
}