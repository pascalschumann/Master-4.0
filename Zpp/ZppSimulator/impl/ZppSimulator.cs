using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.Mrp2;
using Zpp.Util.Graph.impl;
using Zpp.ZppSimulator.impl.Confirmation;
using Zpp.ZppSimulator.impl.Confirmation.impl;
using Zpp.ZppSimulator.impl.CustomerOrder;
using Zpp.ZppSimulator.impl.CustomerOrder.impl;

namespace Zpp.ZppSimulator.impl
{
    public class ZppSimulator : IZppSimulator
    {
        const int _interval = 1440;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string filePattern =
            $"../../../Test/Ordergraphs/Simulation/simulation_";
        
        private readonly IMrp2 _mrp2 = new Mrp2.impl.Mrp2();
        private readonly IConfirmationManager _confirmationManager = new ConfirmationManager();
        private readonly ICustomerOrderCreator _customerOrderCreator = new CustomerOrderCreator();

        public void StartOneCycle(SimulationInterval simulationInterval)
        {
            StartOneCycle(simulationInterval, null);
        }

        public void StartOneCycle(SimulationInterval simulationInterval,
            Quantity customerOrderQuantity)
        {
            // init transactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            _customerOrderCreator.CreateCustomerOrders(simulationInterval, customerOrderQuantity);
            
            _mrp2.StartMrp2();
            
            // TODO: remove this
            DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();
            File.WriteAllText($"{filePattern}_{simulationInterval.StartAt}.txt", demandToProviderGraph.ToString(),
                Encoding.UTF8);
            
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
            mrp2.StartMrp2();
            
            // no confirmations
            
            // persisting cached/created data
            dbTransactionData.PersistDbCache();
        }

        public void StartPerformanceStudy()
        {
            const int maxSimulatingTime = 20160;
            long currentMemoryUsage = GC.GetTotalMemory(false);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // for (int i = 0; i * _interval < maxSimulatingTime; i++)
            for (int i = 0; i * _interval < 5000; i++)
            {
                currentMemoryUsage = GC.GetTotalMemory(false);
                Logger.Info($"CurrentMemoryUsage: {currentMemoryUsage}");
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval, _interval * (i + 1));
                StartOneCycle(simulationInterval, new Quantity(5));
                
                // TODO: Tickzaehlung nur um die Planung innerhalb StartOneCycle und über return zurück
            }
            stopwatch.Stop();
            Logger.Info($"Elapsed cpu ticks: {stopwatch.Elapsed.Ticks}");
        }
        
        
        
    }
}