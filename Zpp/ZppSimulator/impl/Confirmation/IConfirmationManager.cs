namespace Zpp.ZppSimulator.impl.Confirmation
{
    public interface IConfirmationManager
    {
        /**
         * Adapts the states of operations, customerOrders, stockExchanges, purchaseOrderParts
         */
        void CreateConfirmations(SimulationInterval simulationInterval);
        
        void ApplyConfirmations();
    }
}