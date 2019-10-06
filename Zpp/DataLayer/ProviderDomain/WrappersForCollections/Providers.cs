using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.WrappersForCollections;

namespace Zpp.DataLayer.ProviderDomain.WrappersForCollections
{
    /**
     * wraps the collection with all providers
     */
    public class Providers : CollectionWrapperWithStackSet<Provider>, IProviders
    {
        public Providers(List<Provider> list) : base(list)
        {
        }
        
        public Providers(Provider provider) : base(provider)
        {
        }

        public Providers()
        {
        }

        public List<T> GetAllAs<T>()
        {
            List<T> providers = new List<T>();
            foreach (var provider in StackSet)
            {
                providers.Add((T) provider.ToIProvider());
            }

            return providers;
        }

        public bool ProvideMoreThanOrEqualTo(Id articleId, Quantity demandedQuantity)
        {
            return GetProvidedQuantity(articleId).IsGreaterThanOrEqualTo(demandedQuantity);
        }

        public Quantity GetProvidedQuantity(Id articleId)
        {
            Quantity providedQuantity = new Quantity(Quantity.Null());

            foreach (var provider in StackSet)
            {
                if (articleId.Equals(provider.GetArticleId()))
                {
                    providedQuantity.IncrementBy(provider.GetQuantity());
                }
            }

            return providedQuantity;
        }

        public bool IsSatisfied(Quantity demandedQuantity, Id articleId)
        {
            bool isSatisfied = ProvideMoreThanOrEqualTo(articleId, demandedQuantity);
            return isSatisfied;
        }

        public Quantity GetMissingQuantity(Quantity demandedQuantity, Id articleId)
        {
            Quantity missingQuantity = demandedQuantity.Minus(GetProvidedQuantity(articleId));
            if (missingQuantity.IsNegative())
            {
                return Quantity.Null();
            }

            return missingQuantity;
        }

        public IDemands CalculateUnsatisfiedDemands(IDemands demands)
        {
            List<Demand> unSatisfiedDemands = new List<Demand>();
            Dictionary<Provider, Quantity> reservableQuantityToProvider =
                new Dictionary<Provider, Quantity>();
            foreach (var provider in StackSet)
            {
                reservableQuantityToProvider.Add(provider, provider.GetQuantity());
            }

            foreach (var demand in demands.GetAll())
            {
                Quantity neededQuantity = demand.GetQuantity();
                foreach (var provider in StackSet)
                {
                    Quantity reservableQuantity = reservableQuantityToProvider[provider];
                    if (provider.GetArticleId().Equals(demand.GetArticleId()) &&
                        reservableQuantity.IsGreaterThan(Quantity.Null()))
                    {
                        reservableQuantityToProvider[provider] = reservableQuantity
                            .Minus(neededQuantity);
                        neededQuantity = neededQuantity.Minus(reservableQuantity);

                        // neededQuantity < 0
                        if (neededQuantity.IsSmallerThan(Quantity.Null()))
                        {
                            break;
                        }
                        // neededQuantity > 0: continue to provide it
                    }
                }

                if (neededQuantity.IsGreaterThan(Quantity.Null()))
                {
                    unSatisfiedDemands.Add(demand);
                }
            }
            
            return new Demands(unSatisfiedDemands);
        }

        public Provider GetProviderById(Id id)
        {
            // performance: cache this in a dictionary
            foreach (var provider in StackSet)
            {
                if (provider.GetId().Equals(id))
                {
                    return provider;
                }
            }

            return null;
        }

        public List<Provider> GetAllByArticleId(Id id)
        {
            List<Provider> providers = new List<Provider>();
            // performance: cache this in a dictionary
            foreach (var provider in StackSet)
            {
                if (provider.GetArticleId().Equals(id))
                {
                    providers.Add(provider);
                }
            }

            if (providers.Any() == false)
            {
                return null;
            }

            return providers;
        }
    }
}