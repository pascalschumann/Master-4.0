using System.Linq;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.WrappersForCollections;

namespace Zpp.Mrp
{
    public class EntityCollector
    {
        public readonly DemandToProviderTable _demandToProviderTable = new DemandToProviderTable();
        public readonly ProviderToDemandTable _providerToDemandTable = new ProviderToDemandTable();
        public readonly Demands _demands = new Demands();
        public readonly Providers _providers = new Providers();

        public void AddAll(EntityCollector otherEntityCollector)
        {
            if (otherEntityCollector._demands.Any())
            {
                _demands.AddAll(otherEntityCollector._demands);
            }
            if (otherEntityCollector._providers.Any())
            {
                _providers.AddAll(otherEntityCollector._providers);
            }
            if (otherEntityCollector._demandToProviderTable.Any())
            {
                _demandToProviderTable.AddAll(otherEntityCollector._demandToProviderTable);
            }
            if (otherEntityCollector._providerToDemandTable.Any())
            {
                _providerToDemandTable.AddAll(otherEntityCollector._providerToDemandTable);
            }
        }

        public void AddAll(DemandToProviderTable demandToProviderTable)
        {
            _demandToProviderTable.AddAll(demandToProviderTable);
        }
        
        public void AddAll(ProviderToDemandTable providerToDemandTable)
        {
            _providerToDemandTable.AddAll(providerToDemandTable);
        }
        
        public void AddAll(Demands demands)
        {
            _demands.AddAll(demands);
        }
        
        public void AddAll(Providers providers)
        {
            _providers.AddAll(providers);
        }

        public void Add(Demand demand)
        {
            _demands.Add(demand);
        }
        
        public void Add(Provider provider)
        {
            _providers.Add(provider);
        }
        
        public void Add(T_DemandToProvider demandToProvider)
        {
            _demandToProviderTable.Add(demandToProvider);
        }
        
        public void Add(T_ProviderToDemand providerToDemand)
        {
            _providerToDemandTable.Add(providerToDemand);
        }
    }
}