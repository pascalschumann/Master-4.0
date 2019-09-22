using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.Scheduling;
using Zpp.OrderGraph;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.ProviderDomain.Wrappers
{
    public class ProductionOrderOperation : INode
    {
        private readonly T_ProductionOrderOperation _productionOrderOperation;
        private readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
        private Priority _priority = null;

        public ProductionOrderOperation(T_ProductionOrderOperation productionOrderOperation
            )
        {
            _productionOrderOperation = productionOrderOperation;
            
        }

        public OperationBackwardsSchedule ScheduleBackwards(
            OperationBackwardsSchedule lastOperationBackwardsSchedule,
            DueTime dueTimeOfProductionOrder)
        {
            OperationBackwardsSchedule newOperationBackwardsSchedule;

            // case: first run
            if (lastOperationBackwardsSchedule == null)
            {
                newOperationBackwardsSchedule = new OperationBackwardsSchedule(
                    dueTimeOfProductionOrder, _productionOrderOperation.GetDuration(),
                    _productionOrderOperation.GetHierarchyNumber());
            }
            // case: equal hierarchyNumber --> PrOO runs in parallel
            else if (_productionOrderOperation.GetHierarchyNumber()
                .Equals(lastOperationBackwardsSchedule.GetHierarchyNumber()))
            {
                newOperationBackwardsSchedule = new OperationBackwardsSchedule(
                    lastOperationBackwardsSchedule.GetEndOfOperation(),
                    _productionOrderOperation.GetDuration(),
                    _productionOrderOperation.GetHierarchyNumber());
            }
            // case: greaterHierarchyNumber --> PrOO runs after the last PrOO
            else
            {
                if (lastOperationBackwardsSchedule.GetHierarchyNumber()
                    .IsSmallerThan(_productionOrderOperation.GetHierarchyNumber()))
                {
                    throw new MrpRunException(
                        "This is not allowed: hierarchyNumber of lastBackwardsSchedule " +
                        "is smaller than hierarchyNumber of current PrOO (wasn't sorted ?').");
                }

                newOperationBackwardsSchedule = new OperationBackwardsSchedule(
                    lastOperationBackwardsSchedule.GetStartOfOperation(),
                    _productionOrderOperation.GetDuration(),
                    _productionOrderOperation.GetHierarchyNumber());
            }

            // apply schedule on operation
            _productionOrderOperation.EndBackward =
                newOperationBackwardsSchedule.GetEndBackwards().GetValue();
            _productionOrderOperation.StartBackward =
                newOperationBackwardsSchedule.GetStartBackwards().GetValue();

            return newOperationBackwardsSchedule;
        }

        public T_ProductionOrderOperation GetValue()
        {
            return _productionOrderOperation;
        }

        public Id GetResourceSkillId()
        {
            return new Id(_productionOrderOperation.ResourceSkillId);
        }

        public void SetMachine(Resource resource)
        {
            _productionOrderOperation.Resource = resource.GetValue();
        }

        /// <summary>
        /// Todo: Rename to GetPossibleMachines prevent irritation
        /// </summary>
        /// <param name="dbTransactionData"></param>
        /// <returns></returns>
        public List<Resource> GetMachines(IDbTransactionData dbTransactionData)
        {
            return dbTransactionData.GetAggregator()
                .GetResourcesByResourceSkillId(this.GetResourceSkillId());
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(ProductionOrderOperation))
            {
                return false;
            }

            ProductionOrderOperation productionOrderOperation = (ProductionOrderOperation) obj;
            return _productionOrderOperation.GetId()
                .Equals(productionOrderOperation._productionOrderOperation.GetId());
        }

        public override int GetHashCode()
        {
            return _productionOrderOperation.Id.GetHashCode();
        }

        public HierarchyNumber GetHierarchyNumber()
        {
            return _productionOrderOperation.GetHierarchyNumber();
        }

        public DueTime GetDuration()
        {
            return new DueTime(_productionOrderOperation.Duration);
        }

        public Id GetId()
        {
            return _productionOrderOperation.GetId();
        }

        public NodeType GetNodeType()
        {
            return NodeType.Operation;
        }

        public INode GetEntity()
        {
            return this;
        }

        public Id GetProductionOrderId()
        {
            return new Id(_productionOrderOperation.ProductionOrderId);
        }

        public override string ToString()
        {
            return $"{_productionOrderOperation.GetId()}: {_productionOrderOperation.Name}";
        }

        public void SetPriority(Priority priority)
        {
            _priority = priority;
        }

        public Priority GetPriority()
        {
            return _priority;
        }

        public DueTime GetDueTime(IDbTransactionData dbTransactionData)
        {
            // every productionOrderBom whith this operation o1 has the same dueTime
            DueTime dueTime = dbTransactionData.GetAggregator()
                .GetAnyProductionOrderBomByProductionOrderOperation(this)
                .GetDueTime(dbTransactionData);

            return dueTime;
        }

        public ProductionOrder GetProductionOrder(IDbTransactionData dbTransactionData)
        {
            return dbTransactionData.ProductionOrderGetById(GetProductionOrderId());
        }

        /**
         * Every operation needs material to start.
         * @returns the time when material of this operation is available
         */
        public DueTime GetDueTimeOfItsMaterial(IDbTransactionData dbTransactionData)
        {
            DueTime maxDueTime = null;
            foreach (var productionOrderBom in dbTransactionData.GetAggregator()
                .GetAllProductionOrderBomsBy(this))
            {
                Providers providers = dbTransactionData.GetAggregator()
                    .GetAllChildProvidersOf(productionOrderBom);
                if (providers.Count() > 1)
                {
                    throw new MrpRunException("A productionOrderBom can only have one provider !");
                }


                Provider stockExchangeProvider = providers.GetFirst();
                Demands stockExchangeDemands = dbTransactionData.GetAggregator()
                    .GetAllChildDemandsOf(stockExchangeProvider);
                if (maxDueTime == null)
                {
                    maxDueTime = stockExchangeProvider.GetDueTime(dbTransactionData);
                }

                if (stockExchangeDemands.Any() == false)
                    // StockExchangeProvider has no childs (stockExchangeDemands) take that from stockExchangeProvider
                {
                    DueTime childDueTime = stockExchangeProvider.GetDueTime(dbTransactionData);
                    if (childDueTime.IsGreaterThan(maxDueTime))
                    {
                        maxDueTime = childDueTime;
                    }
                }
                else
                    // StockExchangeProvider has childs (stockExchangeDemands)
                {
                    foreach (var stockExchangeDemand in stockExchangeDemands)
                    {
                        DueTime stockExchangeDemandDueTime =
                            stockExchangeDemand.GetDueTime(dbTransactionData);
                        if (stockExchangeDemandDueTime.IsGreaterThan(maxDueTime))
                        {
                            maxDueTime = stockExchangeDemandDueTime;
                        }
                    }
                }
            }

            return maxDueTime;
        }
    }
}