using System;
using System.Collections.Generic;
using System.Linq;
using Zpp.DemandDomain;
using Zpp.WrappersForPrimitives;

namespace Zpp.MachineDomain
{
    public class PriorityRule : IPriorityRule
    {
        public ProductionOrderOperation GetHighestPriorityOperation(DueTime now,
            DueTime minStartNextOfParentProvider,
            List<ProductionOrderOperation> productionOrderOperations,
            IDbTransactionData dbTransactionData)
        {
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                productionOrderOperation.SetPriority(GetPriorityOfProductionOrderOperation(now,
                    productionOrderOperation, dbTransactionData, minStartNextOfParentProvider));
            }

            productionOrderOperations.OrderBy(x => x.GetPriority().GetValue());
            return productionOrderOperations[0];
        }

        public Priority GetPriorityOfProductionOrderOperation(DueTime now,
            ProductionOrderOperation givenProductionOrderOperation,
            IDbTransactionData dbTransactionData, DueTime minStartNextOfParentProvider)
        {
            Dictionary<HierarchyNumber, DueTime> alreadySummedHierarchyNumbers =
                new Dictionary<HierarchyNumber, DueTime>();
            DueTime sumDurationsOfOperations = DueTime.Null();
            List<ProductionOrderOperation> productionOrderOperations = dbTransactionData
                .GetAggregator()
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
                    if (productionOrderOperation.GetDuration()
                        .IsGreaterThan(alreadySummedHierarchyNumber))
                    {
                        sumDurationsOfOperations.IncrementBy(productionOrderOperation.GetDuration()
                            .Minus(alreadySummedHierarchyNumber));
                    }
                }
                else
                {
                    alreadySummedHierarchyNumbers.Add(productionOrderOperation.GetHierarchyNumber(),
                        productionOrderOperation.GetDuration());
                }
            }

            return new Priority(minStartNextOfParentProvider.Minus(now).Minus(sumDurationsOfOperations).GetValue());
        }
    }
}