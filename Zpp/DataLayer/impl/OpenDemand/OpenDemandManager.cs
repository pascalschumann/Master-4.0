using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
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
        private readonly Demands _demands = new Demands();

        private readonly OpenNodes<Demand> _openDemands = new OpenNodes<Demand>();

        private readonly ICacheManager _cacheManager = ZppConfiguration.CacheManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="considerInitialStockLevel">for every initial stock:
        /// a stockExchangeDemand will be created with time -100000,
        /// quantity: initialStockLevel=M_Stock.current</param>
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

        public static bool IsOpen(StockExchangeDemand stockExchangeDemand)
        {
            Quantity reservedQuantity = CalculateReservedQuantity(stockExchangeDemand);
            return stockExchangeDemand.GetQuantity().Minus(reservedQuantity).GetValue() > 0;
        }
        
        /**
         * There initial stock levels defined in M_Stock, to avoid modelling stocks,
         * the initial stock levels are simulated as stockExchangeDemands
         */
        public static void AddInitialStockLevels(IDbTransactionData dbTransactionData)
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

        private static Quantity CalculateReservedQuantity(Demand demand)
        {
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();
            Quantity reservedQuantity = Quantity.Null();

            IEnumerable<ILinkDemandAndProvider> arrowsToDemand = aggregator.GetArrowsTo(demand);
            if (arrowsToDemand != null)
            {
                foreach (var arrowToDemand in arrowsToDemand)
                {
                    reservedQuantity.IncrementBy(arrowToDemand.GetQuantity());
                }
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
                    if (openDemand != null && provider.GetStartTimeBackward()
                            .IsGreaterThanOrEqualTo(openDemand.GetOpenNode().GetStartTimeBackward()))
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