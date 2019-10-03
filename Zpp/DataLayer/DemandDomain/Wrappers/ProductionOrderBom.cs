using System;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Configuration;
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

        public ProductionOrderBom(IDemand demand) : base(demand)
        {
            _productionOrderBom = (T_ProductionOrderBom) _demand;
            // EnsureOperationIsLoadedIfExists();
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
            Provider parentProductionOrder, ProductionOrderOperation productionOrderOperation,
            Quantity quantity)
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
        public override DueTime GetDueTime()
        {
            return GetStartTime();
        }

        public bool HasOperation()
        {
            return _productionOrderBom.ProductionOrderOperationId != null;
        }

        public ProductionOrderOperation GetProductionOrderOperation()
        {
            if (_productionOrderBom.ProductionOrderOperationId == null)
            {
                return null;
            }

            EnsureOperationIsLoadedIfExists();

            return new ProductionOrderOperation(_productionOrderBom.ProductionOrderOperation);
        }

        public DueTime GetStartTimeOfOperation()
        {
            EnsureOperationIsLoadedIfExists();

            if (_productionOrderBom.ProductionOrderOperation?.StartBackward != null)
                // backwards scheduling was already done --> job-shop-scheduling was done
            {
                T_ProductionOrderOperation productionOrderOperation =
                    _productionOrderBom.ProductionOrderOperation;
                DueTime dueTime =
                    new DueTime(productionOrderOperation.StartBackward.GetValueOrDefault());
                return dueTime;
            }
            else
            {
                throw new MrpRunException(
                    "Requesting dueTime for ProductionOrderBom before it was backwards-scheduled.");
            }
        }

        private void SetStartTimeOfOperation(DueTime startTime)
        {
            EnsureOperationIsLoadedIfExists();

            T_ProductionOrderOperation productionOrderOperation =
                _productionOrderBom.ProductionOrderOperation;
            productionOrderOperation.StartBackward = startTime.GetValue();
        }
        
        private void SetEndTimeOfOperation(DueTime endTime)
        {
            EnsureOperationIsLoadedIfExists();

            T_ProductionOrderOperation productionOrderOperation =
                _productionOrderBom.ProductionOrderOperation;
            productionOrderOperation.EndBackward = endTime.GetValue();
        }

        public void EnsureOperationIsLoadedIfExists()
        {
            // load ProductionOrderOperation if not done yet
            if (_productionOrderBom.ProductionOrderOperation == null)
            {
                IDbTransactionData dbTransactionData =
                    ZppConfiguration.CacheManager.GetDbTransactionData();
                Id productionOrderOperationId =
                    new Id(_productionOrderBom.ProductionOrderOperationId.GetValueOrDefault());
                _productionOrderBom.ProductionOrderOperation = dbTransactionData
                    .ProductionOrderOperationGetById(productionOrderOperationId).GetValue();
            }
        }

        public override DueTime GetStartTime()
        {
            EnsureOperationIsLoadedIfExists();
            DueTime transitionTime = new DueTime(OperationBackwardsSchedule.CalculateTransitionTime(
                _productionOrderBom.ProductionOrderOperation.GetDuration()));
            return GetStartTimeOfOperation().Minus(transitionTime);
        }

        public ProductionOrder GetProductionOrder()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
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

        public override Duration GetDuration()
        {
            EnsureOperationIsLoadedIfExists();
            return _productionOrderBom.ProductionOrderOperation.GetDuration();
        }

        public override void SetStartTime(DueTime startTime)
        {
            EnsureOperationIsLoadedIfExists();
            DueTime transitionTime = new DueTime(OperationBackwardsSchedule.CalculateTransitionTime(
                _productionOrderBom.ProductionOrderOperation.GetDuration()));
            // startBackwards
            DueTime startTimeOfOperation = startTime.Plus(transitionTime);
            SetStartTimeOfOperation(startTimeOfOperation);
            // endBackwards
            SetEndTimeOfOperation(startTimeOfOperation.Plus(new DueTime(GetDuration())));
            
        }

        public override void SetDone()
        {
            throw new NotImplementedException();
        }

        public override void SetInProgress()
        {
            throw new NotImplementedException();
        }
    }
}