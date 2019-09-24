using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.DemandDomain.Wrappers
{
    public class CustomerOrderPart : Demand 
    {
        public CustomerOrderPart(IDemand demand) : base(demand)
        {
            
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
    }
}