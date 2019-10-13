using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.Configuration;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;

namespace Zpp.DataLayer.ProviderDomain.Wrappers
{
    /**
     * wraps T_PurchaseOrderPart
     */
    public class PurchaseOrderPart : Provider, IProviderLogic
    {
        private T_PurchaseOrderPart _tPurchaseOrderPart;

        public PurchaseOrderPart(IProvider provider, Demands demands) : base(provider)
        {
            _tPurchaseOrderPart = (T_PurchaseOrderPart) provider;
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

        public override DueTime GetEndTime()
        {
            EnsurePurchaseOrderIsLoaded();
            return new DueTime(_tPurchaseOrderPart.PurchaseOrder.DueTime);
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
                    dbTransactionData.PurchaseOrderGetById(new Id(_tPurchaseOrderPart
                        .PurchaseOrderId));
            }
        }

        public override void SetStartTime(DueTime startTime)
        {
            EnsurePurchaseOrderIsLoaded();
            _tPurchaseOrderPart.PurchaseOrder.DueTime =
                startTime.GetValue() + GetDuration().GetValue();
        }

        public override void SetDone()
        {
            _tPurchaseOrderPart.State = State.Finished;
        }

        public override void SetInProgress()
        {
            _tPurchaseOrderPart.State = State.Producing;
        }

        public override Duration GetDuration()
        {
            // currently only one businessPartner per article TODO: This could be changing
            M_ArticleToBusinessPartner articleToBusinessPartner =
                _dbMasterDataCache.M_ArticleToBusinessPartnerGetAllByArticleId(GetArticleId())[0];
            Duration duration = new Duration(
                 articleToBusinessPartner.TimeToDelivery);
            return duration;
        }

        public override bool IsDone()
        {
            return _tPurchaseOrderPart.State.Equals(State.Finished);
        }

        public override void SetEndTime(DueTime endTime)
        {
            EnsurePurchaseOrderIsLoaded();
            _tPurchaseOrderPart.PurchaseOrder.DueTime =
                endTime.GetValue();
        }

        public override void ClearStartTime()
        {
            EnsurePurchaseOrderIsLoaded();
            _tPurchaseOrderPart.PurchaseOrder.DueTime =
                DueTime.INVALID_DUETIME;

        }

        public override void ClearEndTime()
        {
            EnsurePurchaseOrderIsLoaded();
            _tPurchaseOrderPart.PurchaseOrder.DueTime =
                DueTime.INVALID_DUETIME;

        }
    }
}