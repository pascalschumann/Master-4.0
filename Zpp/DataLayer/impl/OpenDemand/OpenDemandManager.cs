using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Utils;
using Zpp.WrappersForCollections;

namespace Zpp.Mrp.NodeManagement
{
    public class OpenDemandManager : IOpenDemandManager
    {
        // This is only for remembering already added demands !!! (not to persist or anything else)
        private readonly IDemands _demands = new Demands();

        private readonly OpenNodes<Demand> _openDemands = new OpenNodes<Demand>();

        private readonly ICacheManager _cacheManager =
            ZppConfiguration.CacheManager;

        public OpenDemandManager()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            foreach (var stockExchangeDemand in dbTransactionData.StockExchangeDemandsGetAll())
            {
                Quantity reservedQuantity = CalculateReservedQuantity(stockExchangeDemand);
                AddDemand(stockExchangeDemand, reservedQuantity);
            }
        }

        private Quantity CalculateReservedQuantity(Demand demand)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            Quantity reservedQuantity = Quantity.Null();
            dbTransactionData.ProviderToDemandGetAll().Select(x =>
                {
                    reservedQuantity.IncrementBy(x.GetQuantity());
                    return x;
                })
                .Where(x => x.GetDemandId().Equals(demand.GetId()));
            return reservedQuantity;
        }

        public void AddDemand(Demand oneDemand, Quantity reservedQuantity)
        {
            if (_demands.GetDemandById(oneDemand.GetId()) != null)
            {
                throw new MrpRunException("You cannot add an already added demand.");
            }


            // if it has quantity that is not reserved, remember it for later reserving
            if (oneDemand.GetType() == typeof(StockExchangeDemand) &&
                reservedQuantity.IsSmallerThan(oneDemand.GetQuantity()))
            {
                _openDemands.Add(oneDemand.GetArticle(),
                    new OpenNode<Demand>(oneDemand, oneDemand.GetQuantity().Minus(reservedQuantity),
                        oneDemand.GetArticle()));
            }

            // save demand
            _demands.Add(oneDemand);
        }

        /**
         * aka ReserveQuantityOfExistingDemand or satisfyByAlreadyExistingDemand
         */
        public EntityCollector SatisfyProviderByOpenDemand(Provider provider,
            Quantity demandedQuantity)
        {
            if (_openDemands.AnyOpenProvider(provider.GetArticle()))
            {
                EntityCollector entityCollector = new EntityCollector();
                // ths is needed, because openDemands will be removed once they are consumed
                List<OpenNode<Demand>> copyOfOpenDemands = new List<OpenNode<Demand>>();
                copyOfOpenDemands.AddRange(_openDemands.GetOpenProvider(provider.GetArticle()));
                foreach (var openDemand in copyOfOpenDemands)
                {
                    if (openDemand != null && provider.GetDueTime()
                            .IsGreaterThanOrEqualTo(openDemand.GetOpenNode().GetDueTime()))
                    {
                        Quantity remainingQuantity =
                            demandedQuantity.Minus(openDemand.GetOpenQuantity());
                        openDemand.GetOpenQuantity().DecrementBy(demandedQuantity);

                        if (openDemand.GetOpenQuantity().IsNegative() ||
                            openDemand.GetOpenQuantity().IsNull())
                        {
                            _openDemands.Remove(openDemand);
                        }

                        if (remainingQuantity.IsNegative())
                        {
                            remainingQuantity = Quantity.Null();
                        }

                        T_ProviderToDemand providerToDemand = new T_ProviderToDemand()
                        {
                            ProviderId = provider.GetId().GetValue(),
                            DemandId = openDemand.GetOpenNode().GetId().GetValue(),
                            Quantity = demandedQuantity.Minus(remainingQuantity).GetValue()
                        };

                        entityCollector.Add(providerToDemand);
                    }
                }
                return entityCollector;
            }
            else
            {
                return null;
            }

        }

        public void Dispose()
        {
            _openDemands.Clear();
            _demands.Clear();
        }
    }
}