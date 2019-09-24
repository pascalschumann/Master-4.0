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

        void CreateConfirmations(SimulationInterval simulationInterval);
        
        /**
         * - löschen aller Verbindungen zwischen P(SE:W) und D(SE:I)
         * - PrO: D(SE:I) bis P(SE:W) erhalten wenn eine der Ops angefangen
         */
        void ApplyConfirmations();
    }
}