using System;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.DbCache;
using Zpp.Mrp.ProductionManagement;
using Zpp.Mrp.Scheduling;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.DemandDomain.Wrappers
{
    public class ProductionOrderBom : Demand, IDemandLogic
    {
        private readonly T_ProductionOrderBom _productionOrderBom;

        public ProductionOrderBom(IDemand demand) : base(
            demand)
        {
            _productionOrderBom = (T_ProductionOrderBom) _demand;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="articleBom"></param>
        /// <param name="parentProductionOrder"></param>
        /// <param name="dbMasterDataCache"></param>
        /// <param name="quantity">of production article to produce
        /// --> is used for childs as: articleBom.Quantity * quantity</param>
        /// <param name="productionOrderOperation">use already created, null if no one was created before</param>
        /// <returns></returns>
        public static ProductionOrderBom CreateTProductionOrderBom(M_ArticleBom articleBom,
            Provider parentProductionOrder,
            ProductionOrderOperation productionOrderOperation, Quantity quantity)
        {
            T_ProductionOrderBom productionOrderBom = new T_ProductionOrderBom();
            // TODO: Terminierung+Maschinenbelegung
            productionOrderBom.Quantity = articleBom.Quantity * quantity.GetValue();
            productionOrderBom.ProductionOrderParent =
                (T_ProductionOrder) parentProductionOrder.ToIProvider();
            productionOrderBom.ProductionOrderParentId =
                productionOrderBom.ProductionOrderParent.Id;

            // bom is toPurchase if articleBom.Operation == null
            if (productionOrderOperation != null)
            {
                productionOrderBom.ProductionOrderOperation = productionOrderOperation.GetValue();
                productionOrderBom.ProductionOrderOperationId =
                    productionOrderBom.ProductionOrderOperation.Id;
            }

            if (productionOrderOperation == null && articleBom.Operation != null)
            {
                productionOrderBom.ProductionOrderOperation =
                    ProductionManager.CreateProductionOrderOperation(articleBom,
                        parentProductionOrder, quantity);
                productionOrderBom.ProductionOrderOperationId =
                    productionOrderBom.ProductionOrderOperation.Id;
            }


            productionOrderBom.ArticleChild = articleBom.ArticleChild;
            productionOrderBom.ArticleChildId = articleBom.ArticleChildId;

            return new ProductionOrderBom(productionOrderBom);
        }

        public override IDemand ToIDemand()
        {
            return _productionOrderBom;
        }

        public override M_Article GetArticle()
        {
            Id articleId = new Id(_productionOrderBom.ArticleChildId);
            return _dbMasterDataCache.M_ArticleGetById(articleId);
        }

        /**
         * @return:
         *   if ProductionOrderOperation is backwardsScheduled --> EndBackward
         *   else ProductionOrderParent.dueTime
         */
        public override DueTime GetDueTime(IDbTransactionData dbTransactionData = null)
        {
            // load ProductionOrderOperation if not done yet
            if (_productionOrderBom.ProductionOrderOperation == null)
            {
                Id productionOrderOperationId =
                    new Id(_productionOrderBom.ProductionOrderOperationId.GetValueOrDefault());
                _productionOrderBom.ProductionOrderOperation = dbTransactionData
                    .ProductionOrderOperationGetById(productionOrderOperationId).GetValue();
            }

            if (_productionOrderBom.ProductionOrderOperation != null &&
                _productionOrderBom.ProductionOrderOperation.EndBackward != null)
                // backwards scheduling was already done --> job-shop-scheduling was done --> return End
            {
                T_ProductionOrderOperation productionOrderOperation =
                    _productionOrderBom.ProductionOrderOperation;
                DueTime dueTime =
                    new DueTime(productionOrderOperation.StartBackward.GetValueOrDefault() -
                                OperationBackwardsSchedule.CalculateTransitionTime(
                                    productionOrderOperation.GetDuration()));
                return dueTime;
            }
            else
            {
                throw new MrpRunException(
                    "Requesting dueTime for ProductionOrderBom before it was backwards-scheduled.");
            }
        }

        public bool HasOperation()
        {
            return _productionOrderBom.ProductionOrderOperationId != null;
        }

        public ProductionOrderOperation GetProductionOrderOperation(
            IDbTransactionData dbTransactionData)
        {
            if (_productionOrderBom.ProductionOrderOperationId == null)
            {
                return null;
            }

            if (_productionOrderBom.ProductionOrderOperation == null)
                // load it
            {
                _productionOrderBom.ProductionOrderOperation = dbTransactionData
                    .ProductionOrderOperationGetById(new Id(_productionOrderBom
                        .ProductionOrderOperationId.GetValueOrDefault())).GetValue();
            }

            return new ProductionOrderOperation(_productionOrderBom.ProductionOrderOperation);
        }

        public override DueTime GetStartTime(IDbTransactionData dbTransactionData)
        {
            if (_productionOrderBom.ProductionOrderOperationId != null)
            {
                if (_productionOrderBom.ProductionOrderOperation == null)
                    // load it
                {
                    _productionOrderBom.ProductionOrderOperation =
                        dbTransactionData.ProductionOrderOperationGetById(new Id(_productionOrderBom
                            .ProductionOrderOperationId.GetValueOrDefault())).GetValue();
                }

                T_ProductionOrderOperation tProductionOrderOperation =
                    _productionOrderBom.ProductionOrderOperation;
                if (tProductionOrderOperation.StartBackward == null)
                {
                    throw new MrpRunException(
                        "Requesting start time of ProductionOrderBom before it was backwards-scheduled.");
                }

                return new DueTime(tProductionOrderOperation.StartBackward.GetValueOrDefault());
            }
            else
            {
                return null;
            }
        }

        public ProductionOrder GetProductionOrder(IDbTransactionData dbTransactionData)
        {
            if (_productionOrderBom.ProductionOrderParent == null)
            {
                var productionOrder =
                    dbTransactionData
                        .ProductionOrderGetById(new Id(_productionOrderBom.ProductionOrderParentId))
                        .ToIProvider() as T_ProductionOrder;
                if (productionOrder == null)
                {
                    throw new Exception("ProductionOrderBom must have one ProductionOrderParent");
                }

                _productionOrderBom.ProductionOrderParent = productionOrder;
            }


            return new ProductionOrder(_productionOrderBom.ProductionOrderParent);
        }

        public M_ArticleBom GetArticleBom()
        {
            return _dbMasterDataCache.M_ArticleBomGetByArticleChildId(
                new Id(_productionOrderBom.ArticleChildId));
        }
    }
}