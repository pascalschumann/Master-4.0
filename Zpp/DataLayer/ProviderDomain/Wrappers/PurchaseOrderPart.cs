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
        public PurchaseOrderPart(IProvider provider, Demands demands
            ) : base(provider)
        {
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
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            T_PurchaseOrderPart purchaseOrderPart = ((T_PurchaseOrderPart) _provider);
            if (purchaseOrderPart.PurchaseOrder == null)
            {
                purchaseOrderPart.PurchaseOrder =
                    dbTransactionData.PurchaseOrderGetById(
                        new Id(purchaseOrderPart.PurchaseOrderId));
            }

            return new DueTime(purchaseOrderPart.PurchaseOrder.DueTime);
        }

        public override DueTime GetStartTime()
        {
            // currently only one businessPartner per article TODO: This could be changing
            M_ArticleToBusinessPartner articleToBusinessPartner =
                _dbMasterDataCache.M_ArticleToBusinessPartnerGetAllByArticleId(GetArticleId())[0];
            return GetDueTime().Minus(articleToBusinessPartner.TimeToDelivery);
        }

        public override void SetDueTime(DueTime newDueTime)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            T_PurchaseOrderPart purchaseOrderPart = ((T_PurchaseOrderPart) _provider);
            if (purchaseOrderPart.PurchaseOrder == null)
            {
                purchaseOrderPart.PurchaseOrder =
                    dbTransactionData.PurchaseOrderGetById(
                        new Id(purchaseOrderPart.PurchaseOrderId));
            }

            purchaseOrderPart.PurchaseOrder.DueTime = newDueTime.GetValue();
        }

        public override void SetProvided(DueTime atTime)
        {
            throw new System.NotImplementedException();
        }
    }
}