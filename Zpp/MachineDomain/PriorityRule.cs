using System.Collections.Generic;
using Zpp.DemandDomain;
using Zpp.WrappersForPrimitives;

namespace Zpp.MachineDomain
{
    public class PriorityRule : IPriorityRule
    {
        public DueTime GetPriorityOfProductionOrderOperation(DueTime now,
            ProductionOrderBom productionOrderBom, IDbTransactionData dbTransactionData)
        {
            // TODO: This is different from the requirements
            DueTime minStartNextOfParentProvider = productionOrderBom.GetDueTime(dbTransactionData);

            Dictionary<HierarchyNumber, DueTime> alreadySummedHierarchyNumbers =
                new Dictionary<HierarchyNumber, DueTime>();
            DueTime sumDurationsOfOperations = DueTime.Null();
            List<ProductionOrderOperation> productionOrderOperations = dbTransactionData
                .GetAggregator()
                .GetProductionOrderOperationsOfProductionOrder(
                    productionOrderBom.GetProductionOrder());
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                // only later operations, which have a smaller hierarchyNumber, have to be considered
                if (productionOrderOperation.GetHierarchyNumber().IsSmallerThan(productionOrderBom.GetProductionOrderOperation(dbTransactionData).GetHierarchyNumber()))
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

            return minStartNextOfParentProvider.Minus(now).Minus(sumDurationsOfOperations);
        }
    }
}