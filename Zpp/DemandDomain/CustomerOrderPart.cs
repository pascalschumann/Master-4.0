using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.WrappersForPrimitives;


namespace Zpp.DemandDomain
{
    public class CustomerOrderPart : Demand 
    {

        public CustomerOrderPart(IDemand demand, IDbMasterDataCache dbMasterDataCache) : base(demand, dbMasterDataCache)
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

        public override DueTime GetDueTime( )
        {
            Id customerOrderId = new Id(((T_CustomerOrderPart) _demand).CustomerOrderId);
            DueTime dueTime = new DueTime(_dbMasterDataCache.T_CustomerOrderGetById(customerOrderId).DueTime);
            return dueTime;
        }
    }
}