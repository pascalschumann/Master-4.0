using Master40.DB.Data.WrappersForPrimitives;

namespace Zpp.ZppSimulator.impl.CustomerOrder
{
    public interface ICustomerOrderCreator
    {
        /**
         * For Graph generating: Customize delta so, that more customerOrders are created than needed, works only for numbers smaller than 10
         */
        void CreateCustomerOrders(SimulationInterval interval, Quantity customerOrderQuantity);

        /**
         * Uses default delta 0.025
         */
        void CreateCustomerOrders(SimulationInterval interval);
    }
}