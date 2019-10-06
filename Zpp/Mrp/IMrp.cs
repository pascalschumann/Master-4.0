using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.Mrp.impl;
using Zpp.Simulation.impl.Types;

namespace Zpp.Mrp
{
    public interface IMrp
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

        /**
         * For Graph generating: Customize delta so, that more customerOrders are created than needed, works only for numbers smaller than 10
         */
        void CreateOrders(SimulationInterval interval, Quantity customerOrderQuantity);

        /**
         * Uses default delta 0.025
         */
        void CreateOrders(SimulationInterval interval);
    }
}