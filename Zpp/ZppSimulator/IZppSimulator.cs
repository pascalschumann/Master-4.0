using Master40.DB.Data.WrappersForPrimitives;
using Zpp.ZppSimulator.impl;

namespace Zpp.ZppSimulator
{
    public interface IZppSimulator
    {
        void StartOneCycle(SimulationInterval simulationInterval, Quantity customerOrderQuantity);

        void StartOneCycle(SimulationInterval simulationInterval);

        void StartPerformanceStudy();

        void StartTestCycle();
        
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