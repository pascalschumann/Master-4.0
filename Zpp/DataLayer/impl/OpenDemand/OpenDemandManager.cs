using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.Util;

namespace Zpp.DataLayer.impl.OpenDemand
{
    public class OpenDemandManager : IOpenDemandManager
    {
        // This is only for remembering already added demands !!! (not to persist or anything else)
        private readonly IDemands _demands = new Demands();

        private readonly OpenNodes<Demand> _openDemands = new OpenNodes<Demand>();

        private readonly ICacheManager _cacheManager = ZppConfiguration.CacheManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="considerInitialStockLevel">for every initial stock:
        /// a stockExchangeDemand will be created with time -100000,
        /// quantity: initialStockLevel=M_Stock.current</param>
        public OpenDemandManager(bool considerInitialStockLevel)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            if (considerInitialStockLevel)
            {
                ConsiderInitialStockLevel(dbTransactionData);
            }
            
            foreach (var stockExchangeDemand in dbTransactionData.StockExchangeDemandsGetAll())
            {
                Quantity reservedQuantity = CalculateReservedQuantity(stockExchangeDemand);
                AddDemand(stockExchangeDemand, reservedQuantity);
            }
        }
        
        private void ConsiderInitialStockLevel(IDbTransactionData dbTransactionData)
        {
            foreach (var stock in ZppConfiguration.CacheManager.GetMasterDataCache().M_StockGetAll())
            {
                Id articleId = new Id(stock.ArticleForeignKey);
                
                Demand stockExchangeDemand =
                    StockExchangeDemand.CreateStockExchangeStockDemand(articleId,
                        new DueTime(0), new Quantity(stock.Current));
                stockExchangeDemand.SetReadOnly();
                dbTransactionData.DemandsAdd(stockExchangeDemand);
            }
        }

        private Quantity CalculateReservedQuantity(Demand demand)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            Quantity reservedQuantity = Quantity.Null();
            List<T_ProviderToDemand> linksToDemand = dbTransactionData.ProviderToDemandGetAll()
                .Where(x => x.GetDemandId().Equals(demand.GetId())).ToList();
            foreach (var linkToDemand in linksToDemand)
            {
                reservedQuantity.IncrementBy(linkToDemand.GetQuantity());
            }


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
                    if (openDemand != null && provider.GetStartTime()
                            .IsGreaterThanOrEqualTo(openDemand.GetOpenNode().GetStartTime()))
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

                        if (remainingQuantity.IsNull())
                        {
                            break;
                        }
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