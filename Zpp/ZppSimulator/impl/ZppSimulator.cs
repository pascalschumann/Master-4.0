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
using Zpp.Util.Performance;
using Zpp.ZppSimulator.impl.Confirmation;
using Zpp.ZppSimulator.impl.Confirmation.impl;
using Zpp.ZppSimulator.impl.CustomerOrder;
using Zpp.ZppSimulator.impl.CustomerOrder.impl;

namespace Zpp.ZppSimulator.impl
{
    public class ZppSimulator : IZppSimulator
    {
        const int _interval = 1440;
        private readonly PerformanceMonitors _performanceMonitors;
        
        private readonly IMrp2 _mrp2;
        private readonly IConfirmationManager _confirmationManager = new ConfirmationManager();
        private ICustomerOrderCreator _customerOrderCreator = null;

        public ZppSimulator()
        {
            _performanceMonitors = new PerformanceMonitors();
            _mrp2 = new Mrp2.impl.Mrp2(_performanceMonitors);
        }

        public void StartOneCycle(SimulationInterval simulationInterval)
        {
            StartOneCycle(simulationInterval, null);
        }

        public void StartOneCycle(SimulationInterval simulationInterval,
            Quantity customerOrderQuantity)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();

            _performanceMonitors.Start(InstanceToTrack.CreateCustomerOrders);
            _customerOrderCreator.CreateCustomerOrders(simulationInterval, customerOrderQuantity);
            _performanceMonitors.Stop(InstanceToTrack.CreateCustomerOrders);

            // Mrp2
            _performanceMonitors.Start(InstanceToTrack.Mrp2);
            _mrp2.StartMrp2();
            _performanceMonitors.Stop(InstanceToTrack.Mrp2);
            DebuggingTools.PrintStateToFiles(simulationInterval, dbTransactionData, 0);

            // CreateConfirmations
            _performanceMonitors.Start(InstanceToTrack.CreateConfirmations);
            _confirmationManager.CreateConfirmations(simulationInterval);
            _performanceMonitors.Stop(InstanceToTrack.CreateConfirmations);
            DebuggingTools.PrintStateToFiles(simulationInterval, dbTransactionData, 1);

            // ApplyConfirmations
            _performanceMonitors.Start(InstanceToTrack.ApplyConfirmations);
            _confirmationManager.ApplyConfirmations();
            _performanceMonitors.Stop(InstanceToTrack.ApplyConfirmations);
            DebuggingTools.PrintStateToFiles(simulationInterval, dbTransactionData, 2);
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
            

            // init transactionData
            ZppConfiguration.CacheManager.ReloadTransactionData();

            _customerOrderCreator = new CustomerOrderCreator(customerOrderQuantity);
            _customerOrderCreator.CreateCustomerOrders(simulationInterval, customerOrderQuantity);

            // execute mrp2
            _mrp2.StartMrp2();

            DebuggingTools.PrintStateToFiles(simulationInterval,
                ZppConfiguration.CacheManager.GetDbTransactionData(), 0);

            // no confirmations

            // persisting cached/created data
            ZppConfiguration.CacheManager.Persist();
        }

        public void StartPerformanceStudy(bool shouldPersist)
        {
            // TODO: disable if log files
            ZppConfiguration.IsInPerformanceMode = true;
            
            const int maxSimulatingTime = 20160;
            Quantity customerOrderQuantity = new Quantity(ZppConfiguration.CacheManager
                .GetTestConfiguration().CustomerOrderPartQuantity);
            
            
            _customerOrderCreator = new CustomerOrderCreator(customerOrderQuantity);

            // init transactionData
            IDbTransactionData dbTransactionData = ZppConfiguration.CacheManager.ReloadTransactionData();
            
            string performanceLog = "";
            _performanceMonitors.Start();

            for (int i = 0; i * _interval < maxSimulatingTime; i++)
            {

                SimulationInterval simulationInterval =
                    new SimulationInterval(i * _interval, _interval - 1);

                StartOneCycle(simulationInterval);

                if (ZppConfiguration.CacheManager.GetDbTransactionDataArchive().CustomerOrderPartGetAll()
                        .Count() > customerOrderQuantity.GetValue())
                {
                    break;
                }

                performanceLog += _performanceMonitors.ToString();
            }
            _performanceMonitors.Stop();
            performanceLog += $"{_performanceMonitors.ToString()}";
            
            // TODO: enable this again
            // DebuggingTools.PrintStateToFiles(dbTransactionData, true);
            DebuggingTools.WritePerformanceLog(performanceLog);
            
            // persisting cached/created data
            if (shouldPersist)
            {
                ZppConfiguration.CacheManager.Persist();    
            }
            
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
                    new SimulationInterval(i * _interval, _interval - 1);
                StartTestCycle(simulationInterval);
            }
        }
    }
}