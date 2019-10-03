using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.DemandDomain.Wrappers
{
    /**
     * wraps T_StockExchange for T_StockExchange demands
     */
    public class StockExchangeDemand : Demand, IDemandLogic
    {
        private readonly T_StockExchange _tStockExchangeDemand;

        public StockExchangeDemand(IDemand demand) : base(demand)
        {
            _tStockExchangeDemand = (T_StockExchange)demand;
        }

        public override IDemand ToIDemand()
        {
            return (T_StockExchange)_demand;
        }

        public override M_Article GetArticle( )
        {
            Id stockId = new Id(((T_StockExchange) _demand).StockId);
            M_Stock stock = _dbMasterDataCache.M_StockGetById(stockId);
            Id articleId = new Id(stock.ArticleForeignKey);
            return _dbMasterDataCache.M_ArticleGetById(articleId);
        }

        public override DueTime GetDueTime()
        {
            DueTime dueTime = new DueTime(((T_StockExchange) _demand).RequiredOnTime);
            return dueTime;
        }

        public static Demand CreateStockExchangeProductionOrderDemand(M_ArticleBom articleBom, DueTime dueTime)
        {
            IDbMasterDataCache dbMasterDataCache =
                ZppConfiguration.CacheManager.GetMasterDataCache();
            T_StockExchange stockExchange = new T_StockExchange();
            stockExchange.StockExchangeType = StockExchangeType.Demand;
            stockExchange.Quantity = articleBom.Quantity;
            stockExchange.State = State.Created;
            M_Stock stock = dbMasterDataCache.M_StockGetByArticleId(new Id(articleBom.ArticleChildId));
            stockExchange.Stock = stock;
            stockExchange.StockId = stock.Id;
            stockExchange.RequiredOnTime = dueTime.GetValue();
            stockExchange.ExchangeType = ExchangeType.Withdrawal;
            
            StockExchangeDemand stockExchangeDemand =
                new StockExchangeDemand(stockExchange);
            
            return stockExchangeDemand;
        }
        
        public static Demand CreateStockExchangeStockDemand(M_Article article, DueTime dueTime, Quantity quantity)
        {
            IDbMasterDataCache dbMasterDataCache =
                ZppConfiguration.CacheManager.GetMasterDataCache();
            T_StockExchange stockExchange = new T_StockExchange();
            stockExchange.StockExchangeType = StockExchangeType.Demand;
            stockExchange.Quantity = quantity.GetValue();
            stockExchange.State = State.Created;
            M_Stock stock = dbMasterDataCache.M_StockGetByArticleId(article.GetId());
            stockExchange.Stock = stock;
            stockExchange.StockId = stock.Id;
            stockExchange.RequiredOnTime = dueTime.GetValue();
            stockExchange.ExchangeType = ExchangeType.Insert;
            StockExchangeDemand stockExchangeDemand =
                new StockExchangeDemand(stockExchange);
            
            return stockExchangeDemand;
        }

        public bool IsTypeOfInsert()
        {
            return ((T_StockExchange) _demand).ExchangeType.Equals(ExchangeType.Insert);
        }
        
        public Id GetStockId()
        {
            return new Id(((T_StockExchange) _demand).StockId);
        }

        public override DueTime GetStartTime()
        {
            return GetDueTime();
        }

        public override Duration GetDuration()
        {
            return Duration.Null();
        }

        public override void SetStartTime(DueTime startTime)
        {
            _tStockExchangeDemand.RequiredOnTime = startTime.GetValue();
        }
        
        public override void SetDone()
        {
            _tStockExchangeDemand.State = State.Finished;
        }

        public override void SetInProgress()
        {
            _tStockExchangeDemand.State = State.Producing;
        }
    }
}