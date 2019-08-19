using Zpp.DemandDomain;
using Zpp.WrappersForPrimitives;

namespace Zpp.MachineDomain
{
    public interface IPriorityRule
    {
        DueTime GetPriorityOfProductionOrderOperation(DueTime now,
            ProductionOrderBom productionOrderBom, IDbTransactionData dbTransactionData);
    }
}