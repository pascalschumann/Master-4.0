using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.ProviderDomain.Wrappers
{
    /**
     * wraps T_PurchaseOrderPart
     */
    public class PurchaseOrderPart : Provider, IProviderLogic
    {
        private T_PurchaseOrderPart _tPurchaseOrderPart;
        
        public PurchaseOrderPart(IProvider provider, Demands demands
            ) : base(provider)
        {
            _tPurchaseOrderPart = (T_PurchaseOrderPart)provider;
        }

        public override IProvider ToIProvider()
        {
            return (T_PurchaseOrderPart) _provider;
        }

        public override Id GetArticleId()
        {
            Id articleId = new Id(((T_PurchaseOrderPart) _provider).ArticleId);
            return articleId;
        }

        public override void CreateDependingDemands(M_Article article,
            Provider parentProvider, Quantity demandedQuantity)
        {
            throw new System.NotImplementedException();
        }

        public override DueTime GetDueTime()
        {
            EnsurePurchaseOrderIsLoaded();

            return new DueTime(_tPurchaseOrderPart.PurchaseOrder.DueTime);
        }

        public override DueTime GetStartTime()
        {
            // currently only one businessPartner per article TODO: This could be changing
            M_ArticleToBusinessPartner articleToBusinessPartner =
                _dbMasterDataCache.M_ArticleToBusinessPartnerGetAllByArticleId(GetArticleId())[0];
            return GetDueTime().Minus(new DueTime(articleToBusinessPartner.TimeToDelivery));
        }

        public override void SetProvided(DueTime atTime)
        {
            throw new System.NotImplementedException();
        }

        private void EnsurePurchaseOrderIsLoaded()
        {
            if (_tPurchaseOrderPart.PurchaseOrder == null)
            {
                IDbTransactionData dbTransactionData =
                    ZppConfiguration.CacheManager.GetDbTransactionData();
                _tPurchaseOrderPart.PurchaseOrder =
                    dbTransactionData.PurchaseOrderGetById(
                        new Id(_tPurchaseOrderPart.PurchaseOrderId));
            }
        }

        public override void SetStartTime(DueTime dueTime)
        {
            EnsurePurchaseOrderIsLoaded();
            _tPurchaseOrderPart.PurchaseOrder.DueTime = dueTime.GetValue();
        }
    }
}