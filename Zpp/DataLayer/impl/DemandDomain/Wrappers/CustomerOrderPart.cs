using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;

namespace Zpp.DataLayer.impl.DemandDomain.Wrappers
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
            return _dbMasterDataCache.M_ArticleGetById(GetArticleId());
        }

        public override Id GetArticleId()
        {
            return new Id(_customerOrderPart.ArticleId);
        }

        public T_CustomerOrderPart GetValue()
        {
            return (T_CustomerOrderPart)_demand;
        }

        public override Duration GetDuration()
        {
            return Duration.Null();
        }

        public override void SetStartTime(DueTime startTime)
        {
            // is NOT allowed to change
            throw new System.NotImplementedException();
        }

        public override void SetFinished()
        {
            _customerOrderPart.State = State.Finished;
        }

        public override void SetInProgress()
        {
            _customerOrderPart.State = State.InProgress;
        }

        private void EnsureCustomerOrderIsLoaded()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            Id customerOrderId = new Id(_customerOrderPart.CustomerOrderId);
            _customerOrderPart.CustomerOrder =
                dbTransactionData.CustomerOrderGetById(customerOrderId);
        }

        public override DueTime GetEndTime()
        {
            EnsureCustomerOrderIsLoaded();

            DueTime dueTime = new DueTime(_customerOrderPart.CustomerOrder.DueTime);
            return dueTime;
        }

        public override bool IsFinished()
        {
            return _customerOrderPart.State.Equals(State.Finished);
        }

        public override void SetEndTime(DueTime endTime)
        {
            throw new System.NotImplementedException();
        }

        public override void ClearStartTime()
        {
            throw new System.NotImplementedException();
        }

        public override void ClearEndTime()
        {
            throw new System.NotImplementedException();
        }

        public override State? GetState()
        {
            return _customerOrderPart.State;
        }
    }
}