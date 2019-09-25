using System.Linq;
using Zpp.Common.DemandDomain.WrappersForCollections;
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
    }
}