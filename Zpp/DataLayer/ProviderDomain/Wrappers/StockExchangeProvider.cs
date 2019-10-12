using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;

namespace Zpp.DataLayer.ProviderDomain.Wrappers
{
    /**
     * wraps T_StockExchange for T_StockExchange providers
     */
    public class StockExchangeProvider : Provider, IProviderLogic
    {
        private readonly T_StockExchange _stockExchange;
        public StockExchangeProvider(IProvider provider) :
            base(provider)
        {
            _stockExchange = (T_StockExchange) provider;
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

        public Id GetStockId()
        {
            return new Id(((T_StockExchange) _provider).StockId);
        }

        public override void SetProvided(DueTime atTime)
        {
            _stockExchange.State = State.Finished;
            _stockExchange.Time = atTime.GetValue();
        }

        public override void SetStartTime(DueTime startTime)
        {
            _stockExchange.RequiredOnTime = startTime.GetValue();
        }
        
        public override void SetDone()
        {
            _stockExchange.State = State.Finished;
        }

        public override void SetInProgress()
        {
            _stockExchange.State = State.Producing;
        }
        
        public override Duration GetDuration()
        {
            return Duration.Null();
        }

        public override DueTime GetEndTime()
        {
            return new DueTime(_stockExchange.RequiredOnTime);
        }

        public override bool IsDone()
        {
            return _stockExchange.State.Equals(State.Finished);
        }
    }
}