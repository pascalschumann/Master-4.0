using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.DemandDomain;

namespace Zpp.ProviderDomain
{
    public class ProviderManager : IProviderManager
    {
        private readonly IDemandToProviderTable _demandToProviderTable;
        private readonly IProviders _providers;

        public Quantity ReserveQuantityOfExistingProvider(Id demandId, M_Article demandedArticle, Quantity demandedQuantity)
        {
            foreach (var provider in _providers.GetAllByArticleId(demandedArticle.GetId()))
            {
                List<T_DemandToProvider> possibleDemandToProviders = _demandToProviderTable.GetAll()
                    .Where(x => x.ProviderId.Equals(provider.GetId().GetValue())).ToList();
                Quantity alreadyReservedQuantity = Quantity.Null();
                foreach (var possibleDemandToProvider in possibleDemandToProviders)
                {
                    alreadyReservedQuantity.IncrementBy(new Quantity(possibleDemandToProvider.Quantity));
                }
                Quantity freeQuantity = provider.GetQuantity().Minus(alreadyReservedQuantity);
                if (freeQuantity.IsGreaterThan(Quantity.Null()))
                {
                    T_DemandToProvider newDemandToProvider = new T_DemandToProvider();
                    newDemandToProvider.DemandId = demandId.GetValue();
                    newDemandToProvider.ProviderId = provider.GetId().GetValue();
                    _demandToProviderTable.Add(newDemandToProvider);
                    if (freeQuantity.IsGreaterThanOrEqualTo(demandedQuantity))
                    {
                        newDemandToProvider.Quantity = demandedQuantity.GetValue();
                        return Quantity.Null();
                    }
                    Quantity reservedQuantity = demandedQuantity.Minus(freeQuantity);
                    newDemandToProvider.Quantity = reservedQuantity.GetValue();
                    
                    return reservedQuantity;
                }
            }

            return demandedQuantity;
        }

        public Quantity AddProvider(Id demandId, Quantity demandedQuantity, Provider oneProvider)
        {
            _providers.Add(oneProvider);
            T_DemandToProvider demandToProvider = new T_DemandToProvider();
            demandToProvider.DemandId = demandId.GetValue();
            demandToProvider.ProviderId = oneProvider.GetId().GetValue();
            demandToProvider.Quantity = oneProvider.GetQuantity().GetValue();
            _demandToProviderTable.Add(demandToProvider);

            return demandedQuantity.Minus(GetSatisfiedQuantityOfDemand(demandId));
        }

        public Quantity GetSatisfiedQuantityOfDemand(Id demandId)
        {
            throw new NotImplementedException();
        }

        public Quantity GetReservedQuantityOfProvider(Id providerId)
        {
            throw new NotImplementedException();
        }

        public Quantity AddProvider(Demand demand, Provider provider)
        {
            return AddProvider(demand.GetId(), demand.GetQuantity(), provider);
        }
    }
}