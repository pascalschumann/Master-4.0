namespace Zpp.Mrp2.impl.Confirmation
{
    public interface IConfirmationManager
    {
        void RemoveNotStartedProductionOrders();

        void RemoveDemandToProviderConnections();
    }
}