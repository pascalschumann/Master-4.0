using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.Scheduling.impl
{
    /**
     * The difference to DemandToProviderGraph is that no productionOrderBoms nodes are in it. Because scheduling must be done once for oders and once for operations.
     * The DemandToProviderGraph mixes orders and operations by having productionOrderBoms
     * (which are regarding scheduling operations, since productionOrderBoms have no times)
     */
    public class OrderGraph : DirectedGraph, IDirectedGraph<INode>
    {
        public OrderGraph() : base()
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

                INode fromNode = new Node(demand, demandToProvider.GetDemandId());
                INode toNode = new Node(provider, demandToProvider.GetProviderId());
                AddEdge(fromNode, new Edge(demandToProvider, fromNode, toNode));
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
                
                INode fromNode;
                INode toNode;

                if (provider.GetType() == typeof(ProductionOrder))
                {
                    ProductionOrderBom productionOrderBom =
                        ()dbTransactionData.DemandsGetById(providerToDemand.GetDemandId());
                    aggregator.
                    fromNode = new Node(provider, providerToDemand.GetProviderId());
                    toNode = new Node(demand, providerToDemand.GetDemandId());
                }
                else
                {
                    fromNode = new Node(provider, providerToDemand.GetProviderId());
                    toNode = new Node(demand, providerToDemand.GetDemandId());
                }

                
                AddEdge(fromNode, new Edge(providerToDemand, fromNode, toNode));
            }
        }
    }
}