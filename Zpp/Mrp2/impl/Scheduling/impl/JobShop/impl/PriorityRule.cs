using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;

namespace Zpp.Mrp2.impl.Scheduling.impl.JobShop.impl
{
    public class PriorityRule : IPriorityRule
    {
        public ProductionOrderOperation GetHighestPriorityOperation(DueTime now,
            List<ProductionOrderOperation> productionOrderOperations)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            
            if (productionOrderOperations.Any()==false)
            {
                return null;
            }
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                ProductionOrder productionOrder =
                    dbTransactionData.ProductionOrderGetById(productionOrderOperation
                        .GetProductionOrderId());
                // TODO: This is different from specification
                DueTime minStartNextOfParentProvider =
                    productionOrder.GetStartTime();
                
                Priority priority = GetPriorityOfProductionOrderOperation(now,
                    productionOrderOperation, minStartNextOfParentProvider);
                productionOrderOperation.SetPriority(priority);
            }

            return productionOrderOperations.OrderBy(x => x.GetPriority().GetValue()).ToList()[0];
        }

        private Priority GetPriorityOfProductionOrderOperation(DueTime now,
            ProductionOrderOperation givenProductionOrderOperation,
            DueTime minStartNextOfParentProvider)
        {
            IAggregator aggregator =
                ZppConfiguration.CacheManager.GetAggregator();
            
            Dictionary<HierarchyNumber, DueTime> alreadySummedHierarchyNumbers =
                new Dictionary<HierarchyNumber, DueTime>();
            DueTime sumDurationsOfOperations = DueTime.Null();
            List<ProductionOrderOperation> productionOrderOperations = aggregator
                .GetProductionOrderOperationsOfProductionOrder(givenProductionOrderOperation
                    .GetProductionOrderId());
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                // only later operations, which have a smaller hierarchyNumber, have to be considered
                if (productionOrderOperation.GetHierarchyNumber()
                    .IsSmallerThan(givenProductionOrderOperation.GetHierarchyNumber()))
                {
                    continue;
                }

                if (alreadySummedHierarchyNumbers.ContainsKey(productionOrderOperation
                    .GetHierarchyNumber()))
                {
                    DueTime alreadySummedHierarchyNumber =
                        alreadySummedHierarchyNumbers[
                            productionOrderOperation.GetHierarchyNumber()];
                    if (productionOrderOperation.GetDuration().ToDueTime()
                        .IsGreaterThan(alreadySummedHierarchyNumber))
                    {
                        sumDurationsOfOperations.IncrementBy(productionOrderOperation.GetDuration().ToDueTime()
                            .Minus(alreadySummedHierarchyNumber));
                    }
                }
                else
                {
                    alreadySummedHierarchyNumbers.Add(productionOrderOperation.GetHierarchyNumber(),
                        productionOrderOperation.GetDuration().ToDueTime());
                }
            }

            return new Priority(minStartNextOfParentProvider.Minus(now)
                .Minus(sumDurationsOfOperations).GetValue());
        }
    }
}