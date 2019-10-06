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
        const int _interval = 1430;

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

        public void StartTestCycle()
        {
            Quantity customerOrderPartQuantity = new Quantity(ZppConfiguration.CacheManager
                .GetTestConfiguration().CustomerOrderPartQuantity);

            SimulationInterval simulationInterval = new SimulationInterval(0, _interval);
            // StartOneCycle(simulationInterval, customerOrderPartQuantity);
            StartOneCycle(simulationInterval, new Quantity(2));
            
            /*for (int i = 0; i < 100; i++)
            {
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval, _interval * (i + 1));
                StartOneCycle(simulationInterval, new Quantity(5));
            }*/
        }

        public void StartPerformanceStudy()
        {
            const int cycles = 100; // a 5 CO with one COP

            for (int i = 0; i < cycles; i++)
            {
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval, _interval * (i + 1));
                StartOneCycle(simulationInterval, new Quantity(5));
            }
        }
    }
}