using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.ProviderDomain.Wrappers;

namespace Zpp.Scheduling.impl.JobShop
{
    public interface IPriorityRule
    {
        Priority GetPriorityOfProductionOrderOperation(DueTime now,
            ProductionOrderOperation givenProductionOrderOperation,
            DueTime minStartNextOfParentProvider);

        ProductionOrderOperation GetHighestPriorityOperation(DueTime now,
            List<ProductionOrderOperation> productionOrderOperations);
    }
}