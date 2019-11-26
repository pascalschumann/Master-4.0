namespace Zpp.ZppSimulator.impl.Confirmation
{
    public interface IConfirmationManager
    {
        /**
         * Adapts the states of operations, customerOrders, stockExchanges, purchaseOrderParts
         */
        void CreateConfirmations(SimulationInterval simulationInterval);
        
        /**
         * Consult the documentation in diploma thesis of Pascal Schumann
         */
        void ApplyConfirmations();
    }
}