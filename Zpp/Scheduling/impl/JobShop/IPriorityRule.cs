using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.ProviderDomain.Wrappers;

namespace Zpp.Scheduling.impl.JobShop
{
    public interface IPriorityRule
    {

        ProductionOrderOperation GetHighestPriorityOperation(DueTime now,
            List<ProductionOrderOperation> productionOrderOperations);
    }
}