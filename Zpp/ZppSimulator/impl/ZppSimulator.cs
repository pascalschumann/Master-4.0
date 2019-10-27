using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.Mrp2;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Util;
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

        private readonly IMrp2 _mrp2 = new Mrp2.impl.Mrp2();
        private readonly IConfirmationManager _confirmationManager = new ConfirmationManager();
        private ICustomerOrderCreator _customerOrderCreator = null;

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
            DebuggingTools.PrintStateToFiles(simulationInterval, dbTransactionData, 0);

            _confirmationManager.CreateConfirmations(simulationInterval);
            DebuggingTools.PrintStateToFiles(simulationInterval, dbTransactionData, 1);

            _confirmationManager.ApplyConfirmations();
            DebuggingTools.PrintStateToFiles(simulationInterval, dbTransactionData, 2);

            // persisting cached/created data
            ZppConfiguration.CacheManager.Persist();
        }

        /**
         * no confirmations are created and applied
         */
        public void StartTestCycle()
        {
            StartTestCycle(new SimulationInterval(0, _interval));
        }

        /**
         * no confirmations are created and applied
         */
        private void StartTestCycle(SimulationInterval simulationInterval)
        {
            Quantity customerOrderQuantity = new Quantity(ZppConfiguration.CacheManager
                .GetTestConfiguration().CustomerOrderPartQuantity);

            IMrp2 mrp2 = new Mrp2.impl.Mrp2();

            // init transactionData
            ZppConfiguration.CacheManager.ReloadTransactionData();

            _customerOrderCreator = new CustomerOrderCreator(customerOrderQuantity);
            _customerOrderCreator.CreateCustomerOrders(simulationInterval, customerOrderQuantity);

            // execute mrp2
            mrp2.StartMrp2();

            DebuggingTools.PrintStateToFiles(simulationInterval,
                ZppConfiguration.CacheManager.GetDbTransactionData(), 0);

            // no confirmations

            // persisting cached/created data
            ZppConfiguration.CacheManager.Persist();
        }

        public void StartPerformanceStudy()
        {
            // TODO: disable if log files
            ZppConfiguration.IsInPerformanceMode = false;
            
            const int maxSimulatingTime = 20160;
            Quantity customerOrderQuantity = new Quantity(ZppConfiguration.CacheManager
                .GetTestConfiguration().CustomerOrderPartQuantity);
            
            
            _customerOrderCreator = new CustomerOrderCreator(customerOrderQuantity);

            string performanceLog = "";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i * _interval < maxSimulatingTime; i++)
            {
                long currentMemoryUsage = GC.GetTotalMemory(false);
                performanceLog +=
                    $"CurrentMemoryUsage: {DebuggingTools.Prettify(currentMemoryUsage)}" +
                    Environment.NewLine;
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval, _interval);
                StartOneCycle(simulationInterval);

                if (ZppConfiguration.CacheManager.GetDbTransactionData().CustomerOrderPartGetAll()
                        .Count() > customerOrderQuantity.GetValue())
                {
                    break;
                }

                // TODO: Tickzaehlung nur um die Planung innerhalb StartOneCycle und über return zurück
            }

            stopwatch.Stop();
            performanceLog +=
                $"Elapsed cpu ticks: {DebuggingTools.Prettify(stopwatch.Elapsed.Ticks)}" +
                Environment.NewLine;
            DebuggingTools.WritePerformanceLog(performanceLog);
        }

        /**
         * without confirmations
         */
        public void StartMultipleTestCycles()
        {
            const int maxSimulatingTime = 5000;

            // for (int i = 0; i * _interval < maxSimulatingTime; i++)
            for (int i = 0; i * _interval < maxSimulatingTime; i++)
            {
                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval, _interval);
                StartTestCycle(simulationInterval);
            }
        }
    }
}