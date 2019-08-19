using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.LotSize;
using Zpp.ProviderDomain;
using Zpp.SchedulingDomain;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.DemandDomain
{
    public class ProductionOrderBom : Demand, IDemandLogic
    {
        public ProductionOrderBom(IDemand demand, IDbMasterDataCache dbMasterDataCache) : base(
            demand, dbMasterDataCache)
        {
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
        public static ProductionOrderBom CreateProductionOrderBom(M_ArticleBom articleBom,
            Provider parentProductionOrder, IDbMasterDataCache dbMasterDataCache, Quantity quantity,
            ProductionOrderOperation productionOrderOperation)
        {
            T_ProductionOrderBom productionOrderBom = new T_ProductionOrderBom();
            // TODO: Terminierung+Maschinenbelegung
            productionOrderBom.Quantity = articleBom.Quantity * quantity.GetValue();
            productionOrderBom.State = State.Created;
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
                    ProductionOrderOperation.CreateProductionOrderOperation(articleBom,
                        parentProductionOrder);
                productionOrderBom.ProductionOrderOperationId =
                    productionOrderBom.ProductionOrderOperation.Id;
            }


            productionOrderBom.ArticleChild = articleBom.ArticleChild;
            productionOrderBom.ArticleChildId = articleBom.ArticleChildId;

            return new ProductionOrderBom(productionOrderBom, dbMasterDataCache);
        }

        public override IDemand GetIDemand()
        {
            return (T_ProductionOrderBom) _demand;
        }

        public override M_Article GetArticle()
        {
            Id articleId = new Id(((T_ProductionOrderBom) _demand).ArticleChildId);
            return _dbMasterDataCache.M_ArticleGetById(articleId);
        }

        /**
         * @return:
         *   if ProductionOrderOperation is backwardsScheduled --> EndBackward
         *   else ProductionOrderParent.dueTime
         */
        public override DueTime GetDueTime(IDbTransactionData dbTransactionData)
        {
            T_ProductionOrderBom productionOrderBom = ((T_ProductionOrderBom) _demand);

            // load ProductionOrderOperation if not done yet
            if (productionOrderBom.ProductionOrderOperation == null)
            {
                Id productionOrderOperationId =
                    new Id(productionOrderBom.ProductionOrderOperationId.GetValueOrDefault());
                productionOrderBom.ProductionOrderOperation = dbTransactionData
                    .ProductionOrderOperationGetById(productionOrderOperationId);
            }


            DueTime dueTime;
            if (productionOrderBom.ProductionOrderOperation != null &&
                productionOrderBom.ProductionOrderOperation.EndBackward != null)
            {
                // backwards scheduling was already done --> return EndBackward
                dueTime = new DueTime(productionOrderBom.ProductionOrderOperation.EndBackward
                    .GetValueOrDefault());
                return dueTime;
            }
            // backwards scheduling was not yet done --> return dueTime of ProductionOrderParent

            if (productionOrderBom.ProductionOrderParent == null)
            {
                Id productionOrderId = new Id(productionOrderBom.ProductionOrderParentId);
                productionOrderBom.ProductionOrderParent = (T_ProductionOrder) dbTransactionData
                    .ProvidersGetById(productionOrderId).ToIProvider();
            }

            dueTime = new DueTime(productionOrderBom.ProductionOrderParent.DueTime);
            return dueTime;
        }

        public override string GetGraphizString(IDbTransactionData dbTransactionData)
        {
            // Demand(CustomerOrder);20;Truck

            string graphizString;
            T_ProductionOrderBom tProductionOrderBom = ((T_ProductionOrderBom) _demand);
            if (tProductionOrderBom.ProductionOrderOperationId != null)
            {
                if (tProductionOrderBom.ProductionOrderOperation == null)
                {
                    tProductionOrderBom.ProductionOrderOperation =
                        dbTransactionData.ProductionOrderOperationGetById(new Id(tProductionOrderBom
                            .ProductionOrderOperationId.GetValueOrDefault()));
                }

                T_ProductionOrderOperation tProductionOrderOperation =
                    tProductionOrderBom.ProductionOrderOperation;
                graphizString = $"D(PrOB);{base.GetGraphizString(dbTransactionData)};" +
                                $"bs({tProductionOrderOperation.StartBackward});" +
                                $"be({tProductionOrderOperation.EndBackward});\\n{tProductionOrderOperation}";
            }
            else
            {
                graphizString = $"D(PrOB);{base.GetGraphizString(dbTransactionData)}";
            }

            return graphizString;
        }

        public bool HasOperation()
        {
            return ((T_ProductionOrderBom) _demand).ProductionOrderOperationId != null;
        }

        public OperationBackwardsSchedule ScheduleBackwards(
            OperationBackwardsSchedule lastOperationBackwardsSchedule)
        {
            T_ProductionOrderBom tProductionOrderBom = (T_ProductionOrderBom) _demand;

            DueTime TIME_BETWEEN_OPERATIONS =
                new DueTime(tProductionOrderBom.ProductionOrderOperation.Duration * 3);
            int? startBackwards;
            int? endBackwards;
            // case: equal hierarchyNumber --> PrOO runs in parallel
            if (lastOperationBackwardsSchedule.GetHierarchyNumber() == null ||
                (lastOperationBackwardsSchedule.GetHierarchyNumber() != null &&
                 tProductionOrderBom.ProductionOrderOperation.HierarchyNumber.Equals(
                     lastOperationBackwardsSchedule.GetHierarchyNumber().GetValue())))
            {
                endBackwards = lastOperationBackwardsSchedule.GetEndBackwards().GetValue();
                startBackwards = lastOperationBackwardsSchedule.GetEndBackwards().GetValue() -
                                 tProductionOrderBom.ProductionOrderOperation.Duration;
            }
            // case: greaterHierarchyNumber --> PrOO runs after the last PrOO
            else
            {
                if (lastOperationBackwardsSchedule.GetHierarchyNumber().GetValue() <
                    tProductionOrderBom.ProductionOrderOperation.HierarchyNumber)
                {
                    throw new MrpRunException(
                        "This is not allowed: hierarchyNumber of lastBackwardsSchedule " +
                        "is smaller than hierarchyNumber of current PrOO.");
                }

                endBackwards = lastOperationBackwardsSchedule.GetStartBackwards().GetValue();
                startBackwards = lastOperationBackwardsSchedule.GetStartBackwards().GetValue() -
                                 tProductionOrderBom.ProductionOrderOperation.Duration;
            }

            tProductionOrderBom.ProductionOrderOperation.EndBackward = endBackwards;
            tProductionOrderBom.ProductionOrderOperation.StartBackward = startBackwards;

            // create return value
            OperationBackwardsSchedule newOperationBackwardsSchedule =
                new OperationBackwardsSchedule(new DueTime(startBackwards.GetValueOrDefault()),
                    new DueTime(endBackwards.GetValueOrDefault() -
                                TIME_BETWEEN_OPERATIONS.GetValue()),
                    new HierarchyNumber(
                        tProductionOrderBom.ProductionOrderOperation.HierarchyNumber));

            return newOperationBackwardsSchedule;
        }

        public ProductionOrderOperation GetProductionOrderOperation(
            IDbTransactionData dbTransactionData)
        {
            T_ProductionOrderBom productionOrderBom = (T_ProductionOrderBom) _demand;
            if (productionOrderBom.ProductionOrderOperationId == null)
            {
                return null;
            }

            if (productionOrderBom.ProductionOrderOperation == null)
                // load it
            {
                productionOrderBom.ProductionOrderOperation =
                    dbTransactionData.ProductionOrderOperationGetById(
                        new Id(productionOrderBom.ProductionOrderOperationId.GetValueOrDefault()));
            }

            return new ProductionOrderOperation(productionOrderBom.ProductionOrderOperation,
                _dbMasterDataCache);
        }

        public override DueTime GetStartTime(IDbTransactionData dbTransactionData)
        {
            T_ProductionOrderBom productionOrderBom = ((T_ProductionOrderBom) _demand);

            T_ProductionOrderBom tProductionOrderBom = ((T_ProductionOrderBom) _demand);
            if (tProductionOrderBom.ProductionOrderOperationId != null)
            {
                if (tProductionOrderBom.ProductionOrderOperation == null)
                {
                    tProductionOrderBom.ProductionOrderOperation =
                        dbTransactionData.ProductionOrderOperationGetById(new Id(tProductionOrderBom
                            .ProductionOrderOperationId.GetValueOrDefault()));
                }

                T_ProductionOrderOperation tProductionOrderOperation =
                    tProductionOrderBom.ProductionOrderOperation;
                return new DueTime(tProductionOrderOperation.StartBackward.GetValueOrDefault());
            }
            else
            {
                return null;
            }
        }

        public ProductionOrder GetProductionOrder()
        {
            return new ProductionOrder(((T_ProductionOrderBom) _demand).ProductionOrderParent, _dbMasterDataCache);
        }
    }
}