namespace Zpp.Confirmation
{
    public interface IConfirmationManager
    {
        void RemoveNotStartedProductionOrders();

        void RemoveDemandToProviderConnections();
    }
}