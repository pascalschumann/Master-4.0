using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.Utils;
using Zpp.WrappersForCollections;

namespace Zpp.Mrp.NodeManagement
{
    public class ProviderManager : IProviderManager
    {
        private readonly IDemandToProviderTable _demandToProviderTable;
        private readonly IProviderToDemandTable _providerToDemandTable;
        private readonly IProviders _providers;
        private readonly List<Demand> _nextDemands = new List<Demand>();
        private readonly IDbTransactionData _dbTransactionData;

        public ProviderManager(IDbTransactionData dbTransactionData)
        {
            _providers = new Providers();
            _demandToProviderTable = new DemandToProviderTable();
            _providerToDemandTable = new ProviderToDemandTable();
            _dbTransactionData = dbTransactionData;
        }

        public ProviderManager(IDemandToProviderTable demandToProviderTable,
            IProviderToDemandTable providerToDemandTable, IProviders providers)
        {
            _demandToProviderTable = demandToProviderTable;
            _providerToDemandTable = providerToDemandTable;
            _providers = providers;
        }

        public void AddDemandToProvider(T_DemandToProvider demandToProvider)
        {
            _demandToProviderTable.Add(demandToProvider);
        }

        public void AddProvider(Id demandId, Provider oneProvider, Quantity reservedQuantity)
        {
            if (_providers.GetProviderById(oneProvider.GetId()) != null)
            {
                throw new MrpRunException("You cannot add an already added provider.");
            }

            // save provider
            _providers.Add(oneProvider);

            // TODO: this should be done in separate method and be controlled by MrpRun
            // save depending demands
            Demands dependingDemands = oneProvider.GetAllDependingDemands();
            if (dependingDemands != null)
            {
                _nextDemands.AddRange(dependingDemands);
                if (oneProvider.GetType() == typeof(StockExchangeProvider))
                {
                    _providerToDemandTable.AddAll(oneProvider.GetProviderToDemandTable());
                }
                else
                {
                    foreach (var dependingDemand in dependingDemands)
                    {
                        _providerToDemandTable.Add(oneProvider, dependingDemand.GetId(),
                            dependingDemand.GetQuantity());
                    }
                }
            }
        }

        public Quantity GetSatisfiedQuantityOfDemand(Id demandId)
        {
            Quantity sum = Quantity.Null();
            foreach (var demandToProvider in _demandToProviderTable)
            {
                if (demandToProvider.DemandId.Equals(demandId.GetValue()))
                {
                    sum.IncrementBy(new Quantity(demandToProvider.Quantity));
                }
            }

            return sum;
        }

        public Quantity GetReservedQuantityOfProvider(Id providerId)
        {
            Quantity sum = Quantity.Null();
            foreach (var demandToProvider in _demandToProviderTable)
            {
                if (demandToProvider.ProviderId.Equals(providerId.GetValue()))
                {
                    sum.IncrementBy(new Quantity(demandToProvider.Quantity));
                }
            }

            return sum;
        }

        public Demands GetNextDemands()
        {
            if (_nextDemands.Any() == false)
            {
                return null;
            }

            Demands nextDemands = new Demands(_nextDemands);
            _nextDemands.Clear();
            return nextDemands;
        }

        public IDemandToProviderTable GetDemandToProviderTable()
        {
            return _demandToProviderTable;
        }

        public IProviderToDemandTable GetProviderToDemandTable()
        {
            return _providerToDemandTable;
        }

        public bool IsSatisfied(Demand demand)
        {
            return GetSatisfiedQuantityOfDemand(demand.GetId())
                .IsGreaterThanOrEqualTo(demand.GetQuantity());
        }

        public IProviders GetProviders()
        {
            return _providers;
        }
    }
}