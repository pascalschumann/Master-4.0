using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.DemandDomain.Wrappers
{
    public class CustomerOrderPart : Demand
    {
        private T_CustomerOrderPart _customerOrderPart;
        
        public CustomerOrderPart(IDemand demand) : base(demand)
        {
            _customerOrderPart = (T_CustomerOrderPart) demand;
        }

        public override IDemand ToIDemand()
        {
            return (T_CustomerOrderPart)_demand;
        }

        public override M_Article GetArticle()
        {
            Id articleId = new Id(((T_CustomerOrderPart) _demand).ArticleId);
            return _dbMasterDataCache.M_ArticleGetById(articleId);
        }

        public override DueTime GetDueTime()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            
            T_CustomerOrderPart customerOrderPart = ((T_CustomerOrderPart) _demand);
            if (customerOrderPart.CustomerOrder != null)
            {
                return new DueTime(customerOrderPart.CustomerOrder.DueTime);
            }
            Id customerOrderId = new Id(customerOrderPart.CustomerOrderId);
            customerOrderPart.CustomerOrder =
                dbTransactionData.T_CustomerOrderGetById(customerOrderId);
            DueTime dueTime = new DueTime(customerOrderPart.CustomerOrder.DueTime);
            return dueTime;
        }

        public override DueTime GetStartTime()
        {
            return GetDueTime();
        }

        public T_CustomerOrderPart GetValue()
        {
            return (T_CustomerOrderPart)_demand;
        }

        public override Duration GetDuration()
        {
            return Duration.Null();
        }

        public override void SetStartTime(DueTime dueTime)
        {
            throw new System.NotImplementedException();
        }

        public override void SetDone()
        {
            _customerOrderPart.State = State.Finished;
        }

        public override void SetInProgress()
        {
            _customerOrderPart.State = State.Producing;
        }
    }
}