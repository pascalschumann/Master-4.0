using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.Mrp2;
using Zpp.ZppSimulator.impl.Confirmation;
using Zpp.ZppSimulator.impl.Confirmation.impl;
using Zpp.ZppSimulator.impl.CustomerOrder;
using Zpp.ZppSimulator.impl.CustomerOrder.impl;

namespace Zpp.ZppSimulator.impl
{
    public class ZppSimulator : IZppSimulator
    {
        const int _interval = 1440;
        
        private readonly IMrp2 _mrp2 = new Mrp2.impl.Mrp2();
        private readonly IConfirmationManager _confirmationManager = new ConfirmationManager();
        private readonly ICustomerOrderCreator _customerOrderCreator = new CustomerOrderCreator();

        public void StartOneCycle(SimulationInterval simulationInterval)
        {
            StartOneCycle(simulationInterval);
        }

        public void StartOneCycle(SimulationInterval simulationInterval,
            Quantity customerOrderQuantity)
        {
            // init transactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            _customerOrderCreator.CreateCustomerOrders(simulationInterval, customerOrderQuantity);
            
            _mrp2.StartMrp2();
            
            _confirmationManager.CreateConfirmations(simulationInterval);

            _confirmationManager.ApplyConfirmations();

            // persisting cached/created data
            dbTransactionData.PersistDbCache();
            
        }

        /**
         * no confirmations are created and applied
         */
        public void StartTestCycle()
        {
            Quantity customerOrderQuantity = new Quantity(ZppConfiguration.CacheManager
                .GetTestConfiguration().CustomerOrderPartQuantity);

            SimulationInterval simulationInterval = new SimulationInterval(0, _interval);
            
            IMrp2 mrp2 = new Mrp2.impl.Mrp2();

            // init transactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            _customerOrderCreator.CreateCustomerOrders(simulationInterval, customerOrderQuantity);

            // execute mrp2
            Demands unsatisfiedCustomerOrderParts = ZppConfiguration.CacheManager.GetAggregator()
                .GetPendingCustomerOrderParts();
            mrp2.ManufacturingResourcePlanning(unsatisfiedCustomerOrderParts);
            
            // no confirmations
            
            // persisting cached/created data
            dbTransactionData.PersistDbCache();
        }

        public void StartPerformanceStudy()
        {
            const int maxSimulatingTime = 20160;

            for (int i = 0; i * _interval < maxSimulatingTime; i++)
            {
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval, _interval * (i + 1));
                StartOneCycle(simulationInterval);
            }
        }
        
        
        
    }
}