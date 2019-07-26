using System;
using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DemandDomain;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.LotSize;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.ProviderDomain
{
    /**
     * wraps T_StockExchange for T_StockExchange providers
     */
    public class StockExchangeProvider : Provider, IProviderLogic
    {
        public StockExchangeProvider(IProvider provider, IDbMasterDataCache dbMasterDataCache) :
            base(provider, dbMasterDataCache)
        {
        }

        public override IProvider ToIProvider()
        {
            return (T_StockExchange) _provider;
        }

        public override Id GetArticleId()
        {
            Id stockId = new Id(((T_StockExchange) _provider).StockId);
            M_Stock stock = _dbMasterDataCache.M_StockGetById(stockId);
            return new Id(stock.ArticleForeignKey);
        }

        public override void CreateNeededDemands(M_Article article,
            IDbTransactionData dbTransactionData, IDbMasterDataCache dbMasterDataCache,
            Provider parentProvider, Quantity quantity)
        {
            if (quantity.IsNull())
            {
                return;
            }
            _dependingDemands = new Demands();
            Demand stockExchangeDemand =
                StockExchangeDemand.CreateStockExchangeStockDemand(article, GetDueTime(), quantity,
                    _dbMasterDataCache);
            if (stockExchangeDemand.GetQuantity().IsSmallerThan(quantity))
            {
                throw new MrpRunException($"Created demand should have not a smaller " +
                                          $"quantity ({stockExchangeDemand.GetQuantity()}) " +
                                          $"than the needed quantity ({quantity}).");
            }
            _dependingDemands.Add(stockExchangeDemand);
        }

        /**
         * returns a provider, which can be a stockExchangeProvider, if article can be fulfilled by stock, else
         * a productionOrder/purchaseOrderPart
         */
        public static Provider CreateStockProvider(M_Article article, DueTime dueTime,
            Quantity demandedQuantity, IDbMasterDataCache dbMasterDataCache,
            IDbTransactionData dbTransactionData)
        {
            M_Stock stock = dbMasterDataCache.M_StockGetByArticleId(article.GetId());
            if (stock.Current <= 0)
            {
                return null;
            }
            Quantity currentStockQuantity = new Quantity(stock.Current);
            Quantity providedQuantityByStock =
                CalcQuantityProvidedByProvider(currentStockQuantity, demandedQuantity);
            if (! providedQuantityByStock.Equals(Quantity.Null()))
            {
                T_StockExchange stockExchange = new T_StockExchange();
                stockExchange.StockExchangeType = StockExchangeType.Provider;
                stockExchange.Quantity = providedQuantityByStock.GetValue();
                stockExchange.State = State.Created;

                stockExchange.Stock = stock;
                stockExchange.StockId = stock.Id;
                stockExchange.RequiredOnTime = dueTime.GetValue();
                stockExchange.ExchangeType = ExchangeType.Withdrawal;
                StockExchangeProvider stockExchangeProvider =
                    new StockExchangeProvider(stockExchange, dbMasterDataCache);

                // Update stock
                stock.Current = currentStockQuantity.Minus(providedQuantityByStock).GetValue();
                if (stock.Current <= stock.Min)
                {
                    Quantity missingQuantity = new Quantity(stock.Max - stock.Current);
                    if (missingQuantity.IsNegative() || missingQuantity.IsNull())
                    { // buildArticle has max == zero --> will always be negative (null if current is also 0)
                        missingQuantity = Quantity.Null();
                    }

                    if (stock.Current + missingQuantity.GetValue() < stock.Min)
                    {
                        throw new MrpRunException($"Stock will not be refilled correctly.");
                    }
                    stockExchangeProvider.CreateNeededDemands(article, dbTransactionData,
                        dbMasterDataCache, stockExchangeProvider, missingQuantity);
                }

                return stockExchangeProvider;
            }

            return null;
        }

        public override string GetGraphizString()
        {
            // Demand(CustomerOrder);20;Truck
            string exchangeType = Constants.EnumToString(((T_StockExchange)_provider).ExchangeType, typeof(ExchangeType));
            string graphizString = $"P(SE:{exchangeType[0]});{GetQuantity()};{GetArticle().Name}";
            return graphizString;
        }
    }
}