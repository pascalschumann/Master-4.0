using System;
using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DemandDomain;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.LotSize;
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

        public override Demands CreateNeededDemands(M_Article article,
            IDbTransactionData dbTransactionData, IDbMasterDataCache dbMasterDataCache,
            Provider parentProvider, Quantity quantity)
        {
            _demands = new Demands();
            _demands.Add(StockExchangeDemand.CreateStockExchangeStockDemand(article, GetDueTime(), quantity, _dbMasterDataCache));
            return _demands;
        }

        public static Provider CreateStockProvider(M_Article article, DueTime dueTime, Quantity demandedQuantity,
            IDbMasterDataCache dbMasterDataCache, IDbTransactionData dbTransactionData)
        {
            M_Stock stock = dbMasterDataCache.M_StockGetByArticleId(article.GetId());
            Quantity currentStockQuantity = new Quantity(stock.Current);
            Quantity providedQuantityByStock =
                CalcQuantityProvidedByProvider(currentStockQuantity, demandedQuantity);
            if (providedQuantityByStock != null)
            {
                T_StockExchange stockExchange = new T_StockExchange();
                stockExchange.Provider = new T_Provider();
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
                if (stock.Current < stock.Min)
                {
                    stockExchangeProvider.CreateNeededDemands(article,
                        dbTransactionData, dbMasterDataCache, stockExchangeProvider, new Quantity(stock.Max - stock.Current));
                }

                return stockExchangeProvider;
            }

            return null;
        }
        
    }
}