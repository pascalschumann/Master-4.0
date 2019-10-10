using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.Mrp;
using Zpp.Simulation.impl.Types;

namespace Zpp.ZppSimulator.impl
{
    public class ZppSimulator : IZppSimulator
    {
        const int _interval = 1440;

        public void StartOneCycle(SimulationInterval simulationInterval)
        {
            StartOneCycle(simulationInterval, null);
        }

        public void StartOneCycle(SimulationInterval simulationInterval,
            Quantity customerOrderQuantity)
        {
            IMrp mrp = new Mrp.impl.Mrp();

            // init transactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            mrp.CreateOrders(simulationInterval, customerOrderQuantity);

            // execute mrp2
            Demands unsatisfiedCustomerOrderParts = ZppConfiguration.CacheManager.GetAggregator()
                .GetPendingCustomerOrderParts();
            mrp.ManufacturingResourcePlanning(unsatisfiedCustomerOrderParts);

            mrp.CreateConfirmations(simulationInterval);

            mrp.ApplyConfirmations();

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
            
            IMrp mrp = new Mrp.impl.Mrp();

            // init transactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            mrp.CreateOrders(simulationInterval, customerOrderQuantity);

            // execute mrp2
            Demands unsatisfiedCustomerOrderParts = ZppConfiguration.CacheManager.GetAggregator()
                .GetPendingCustomerOrderParts();
            mrp.ManufacturingResourcePlanning(unsatisfiedCustomerOrderParts);
            
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