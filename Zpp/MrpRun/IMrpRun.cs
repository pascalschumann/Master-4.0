using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Mrp.StockManagement;
using Zpp.Simulation.Types;

namespace Zpp.Mrp
{
    public interface IMrpRun
    {
        void Start(bool withForwardScheduling = true);

        void ManufacturingResourcePlanning(IDemands dbDemands, int count,
            bool withForwardScheduling);
        
        void MaterialRequirementsPlanning(Demand demand, IStockManager stockManager);

        void ScheduleBackward();
        
        void ScheduleForward(int count);

        void JobShopScheduling();

        void ApplyConfirmations();
        
        void CreateConfirmations(SimulationInterval simulationInterval);
    }
}