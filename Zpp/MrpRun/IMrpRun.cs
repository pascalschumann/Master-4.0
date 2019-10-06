using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Mrp.StockManagement;
using Zpp.Simulation.Types;

namespace Zpp.Mrp
{
    public interface IMrpRun
    {
        void ManufacturingResourcePlanning(IDemands dbDemands);
        
        EntityCollector MaterialRequirementsPlanning(Demand demand, IProviderManager providerManager);

        void ScheduleBackward();
        
        void ScheduleForward();

        void JobShopScheduling();
        
        /**
         * Adapts the states of operations, customerOrders, stockExchanges, purchaseOrderParts
         */
        void CreateConfirmations(SimulationInterval simulationInterval);
        
        /**
         * - löschen aller Verbindungen zwischen P(SE:W) und D(SE:I)
         * - PrO: D(SE:I) bis P(SE:W) erhalten wenn eine der Ops angefangen
         */
        void ApplyConfirmations();

        void CreateOrders(SimulationInterval interval);
    }
}