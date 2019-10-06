using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.ProviderDomain;

namespace Zpp.Util.Graph.impl
{
    public class DemandToProviderDirectedGraph : DirectedGraph, IDirectedGraph<INode>
    {
        public DemandToProviderDirectedGraph() : base()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            
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

                INode fromNode = new Node(provider, providerToDemand.GetProviderId());
                INode toNode = new Node(demand, providerToDemand.GetDemandId());
                AddEdge(fromNode, new Edge(providerToDemand, fromNode, toNode));
            }
        }
    }
}