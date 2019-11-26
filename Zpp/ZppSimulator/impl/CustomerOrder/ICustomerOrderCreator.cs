using Master40.DB.Data.WrappersForPrimitives;

namespace Zpp.ZppSimulator.impl.CustomerOrder
{
    public interface ICustomerOrderCreator
    {
        /**
         * Exact order generating
         */
        void CreateCustomerOrders(SimulationInterval interval, Quantity customerOrderQuantity);
        
    }
}