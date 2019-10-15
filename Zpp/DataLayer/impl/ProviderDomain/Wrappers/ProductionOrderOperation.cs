using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Mrp2.impl.Scheduling.impl.JobShop;
using Zpp.Mrp2.impl.Scheduling.impl.JobShop.impl;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.DataLayer.impl.ProviderDomain.Wrappers
{
    public class ProductionOrderOperation : IScheduleNode
    {
        private readonly T_ProductionOrderOperation _productionOrderOperation;
        private Priority _priority = null;

        public ProductionOrderOperation(T_ProductionOrderOperation productionOrderOperation
            )
        {
            _productionOrderOperation = productionOrderOperation;
            
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
        public List<Resource> GetMachines()
        {
            return ZppConfiguration.CacheManager.GetAggregator()
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

        public Duration GetDuration()
        {
            return new Duration(_productionOrderOperation.Duration);
        }

        public Id GetId()
        {
            return _productionOrderOperation.GetId();
        }

        public NodeType GetNodeType()
        {
            return NodeType.Operation;
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

        public ProductionOrder GetProductionOrder()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            return dbTransactionData.ProductionOrderGetById(GetProductionOrderId());
        }

        /**
         * Every operation needs material to start.
         * @returns the time when material of this operation is available
         */
        public DueTime GetEarliestPossibleStartTime()
        {
           
            DueTime maxDueTime = null;
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();
            
            foreach (var productionOrderBom in 
                aggregator.GetAllProductionOrderBomsBy(this))
            {
                DueTime earliestDueTime = aggregator.GetEarliestPossibleStartTimeOf((ProductionOrderBom)productionOrderBom);
                if (maxDueTime == null || earliestDueTime.IsGreaterThan(maxDueTime))
                {
                    maxDueTime = earliestDueTime;

                }
            }

            return maxDueTime;
        }
        
        public void SetDone()
        {
            _productionOrderOperation.ProducingState = ProducingState.Finished;
        }

        public void SetInProgress()
        {
            _productionOrderOperation.ProducingState = ProducingState.Producing;
        }
        
        public DueTime GetEndTime()
        {
            if (_productionOrderOperation.EndBackward == null)
            {
                throw new MrpRunException("Cannot request endTime before operation is scheduled.");
            }
            return new DueTime(_productionOrderOperation.EndBackward.GetValueOrDefault());
        }
        
        public DueTime GetStartTime()
        {
            if (_productionOrderOperation.StartBackward == null)
            {
                return null;
            }
            DueTime transitionTime =
                new DueTime(
                    TransitionTimer.CalculateTransitionTime(GetDuration()));
            DueTime startTimeOfOperation =
                new DueTime(_productionOrderOperation.StartBackward.GetValueOrDefault());
            DueTime startTime = startTimeOfOperation.Minus(transitionTime);
            return startTime;
        }

        public void SetStartTime(DueTime startTime)
        {
            DueTime transitionTime =
                new DueTime(
                    TransitionTimer.CalculateTransitionTime(GetDuration()));
            // startBackwards
            DueTime startTimeOfOperation = startTime.Plus(transitionTime);
            _productionOrderOperation.StartBackward = startTimeOfOperation.GetValue();
            // endBackwards
            _productionOrderOperation.EndBackward = startTimeOfOperation.GetValue() + GetDuration().GetValue();
        }

        Duration IScheduleNode.GetDuration()
        {
            return _productionOrderOperation.GetDuration();
        }

        public bool IsDone()
        {
            return _productionOrderOperation.ProducingState.Equals(ProducingState.Finished);
        }

        public void SetEndTime(DueTime endTime)
        {
            DueTime transitionTime =
                new DueTime(
                    TransitionTimer.CalculateTransitionTime(GetDuration()));
            // endBackwards
            _productionOrderOperation.EndBackward = endTime.GetValue();
            // startBackwards
            DueTime startTimeOfOperation = endTime.Minus(GetDuration().ToDueTime());
            _productionOrderOperation.StartBackward = startTimeOfOperation.GetValue();
        }

        public void ClearStartTime()
        {
            _productionOrderOperation.StartBackward = null;
        }

        public void ClearEndTime()
        {
            _productionOrderOperation.EndBackward = null;
        }
    }
}