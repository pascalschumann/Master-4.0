using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.Util;

namespace Zpp.DataLayer.impl.ProviderDomain.Wrappers
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
            if (_tPurchaseOrderPart.IsReadOnly)
            {
                throw new MrpRunException("A readOnly entity cannot be changed anymore.");
            }
            EnsurePurchaseOrderIsLoaded();
            _tPurchaseOrderPart.PurchaseOrder.DueTime =
                startTime.GetValue() + GetDuration().GetValue();
        }

        public override void SetFinished()
        {
            if (_tPurchaseOrderPart.IsReadOnly)
            {
                throw new MrpRunException("A readOnly entity cannot be changed anymore.");
            }
            _tPurchaseOrderPart.State = State.Finished;
        }

        public override void SetInProgress()
        {
            if (_tPurchaseOrderPart.IsReadOnly)
            {
                throw new MrpRunException("A readOnly entity cannot be changed anymore.");
            }
            _tPurchaseOrderPart.State = State.InProgress;
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

        public override bool IsFinished()
        {
            return _tPurchaseOrderPart.State.Equals(State.Finished);
        }

        public override void SetEndTime(DueTime endTime)
        {
            if (_tPurchaseOrderPart.IsReadOnly)
            {
                throw new MrpRunException("A readOnly entity cannot be changed anymore.");
            }
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

        public override State? GetState()
        {
            return _tPurchaseOrderPart.State;
        }
    }
}