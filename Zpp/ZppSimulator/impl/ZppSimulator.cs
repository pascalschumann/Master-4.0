using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.Simulation.Types;

namespace Zpp.ZppSimulator.impl
{
    public class ZppSimulator : IZppSimulator
    {
        const int _interval = 1430;

        public void StartOneCycle(SimulationInterval simulationInterval)
        {
            IMrpRun mrpRun = new MrpRun();

            // init transactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            
            mrpRun.CreateOrders(simulationInterval);

            // execute mrp2
            Demands unsatisfiedCustomerOrderParts = ZppConfiguration.CacheManager.GetAggregator()
                .GetPendingCustomerOrderParts();
            mrpRun.ManufacturingResourcePlanning(unsatisfiedCustomerOrderParts);

            mrpRun.CreateConfirmations(simulationInterval);

            mrpRun.ApplyConfirmations();

            // persisting cached/created data
            dbTransactionData.PersistDbCache();
        }

        public void StartTestCycle()
        {
            int cycles = ZppConfiguration.CacheManager
                .GetTestConfiguration().CustomerOrderPartQuantity;

            for (int i = 0; i < cycles; i++)
            {
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval,
                        _interval * (i + 1));
                StartOneCycle(simulationInterval);
            }
        }

        public void StartPerformanceStudy()
        {
            const int cycles = 100; // approximately equivalent to 500 COPs

            for (int i = 0; i < cycles; i++)
            {
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval,
                        _interval * (i + 1));
                StartOneCycle(simulationInterval);
            }
        }
    }
}