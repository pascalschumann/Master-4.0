using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer;
using Zpp.OrderGraph;
using Zpp.WrappersForCollections;

namespace Zpp.Mrp
{
    public class EntityCollector
    {
        private readonly DemandToProviderTable _demandToProviderTable = new DemandToProviderTable();
        private readonly ProviderToDemandTable _providerToDemandTable = new ProviderToDemandTable();
        private readonly Demands _demands = new Demands();
        private readonly Providers _providers = new Providers();

        public void AddAll(EntityCollector otherEntityCollector)
        {
            if (otherEntityCollector == null)
            {
                return;
            }
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

        public DemandToProviderTable GetDemandToProviderTable()
        {
            return _demandToProviderTable;
        }

        public ProviderToDemandTable GetProviderToDemandTable()
        {
            return _providerToDemandTable;
        }

        public Demands GetDemands()
        {
            return _demands;
        }

        public Providers GetProviders()
        {
            return _providers;
        }

        public bool IsSatisfied(IDemandOrProvider demandOrProvider)
        {
            return GetRemainingQuantity(demandOrProvider).Equals(Quantity.Null());
        }

        public Quantity GetRemainingQuantity(IDemandOrProvider demandOrProvider)
        {
            Quantity reservedQuantity = demandOrProvider.GetQuantity()
                .Minus(SumReservedQuantity(demandOrProvider));
            if (reservedQuantity.IsNegative())
            {
                return Quantity.Null();
            }
            else
            {
                return reservedQuantity;
            }
        }

        private Quantity SumReservedQuantity(IDemandOrProvider demandOrProvider)
        {
            if (demandOrProvider.GetNodeType().Equals(NodeType.Demand))
            {
                return SumReservedQuantity(Demand.AsDemand(demandOrProvider));
            }
            else
            {
                return SumReservedQuantity(Provider.AsProvider(demandOrProvider));
            }
        }

        public Quantity SumReservedQuantity(Demand demand)
        {
            return SumReservedQuantity(
                _demandToProviderTable.Where(x => x.GetDemandId().Equals(demand.GetId())));
        }

        public Quantity SumReservedQuantity(Provider provider)
        {
            return SumReservedQuantity(
                _providerToDemandTable.Where(x => x.GetProviderId().Equals(provider.GetId())));
        }

        /// <summary>
        /// Sums the reserved quantity
        /// </summary>
        /// <param name="demandAndProviderLinks">ATTENTION: filter this for demand/provider id before,
        /// else the sum can could be higher than expected</param>
        /// <returns></returns>
        public static Quantity SumReservedQuantity(
            IEnumerable<ILinkDemandAndProvider> demandAndProviderLinks)
        {
            Quantity reservedQuantity = Quantity.Null();
            foreach (var demandAndProviderLink in demandAndProviderLinks)
            {
                reservedQuantity.IncrementBy(demandAndProviderLink.GetQuantity());
            }

            return reservedQuantity;
        }
    }
}