using System;
using System.Linq;
using Master40.DB.Data.Context;
using Master40.DB.Data.Helper;
using Master40.DB.DataModel;
using Master40.SimulationCore.DistributionProvider;
using Master40.SimulationCore.Environment.Options;
using Priority_Queue;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.NodeManagement;
using Zpp.Mrp.StockManagement;
using Zpp.Simulation;
using Zpp.Simulation.Types;
using Zpp.Test.Configuration.Scenarios;
using Zpp.Utils;
using Zpp.Utils.Queue;

namespace Zpp.Mrp
{
    public static class MrpRun
    {
        private static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();

        /**
         * Only at start the demands are customerOrders
         */
        public static void Start(ProductionDomainContext productionDomainContext,
            bool withForwardScheduling = true)
        {
            OrderGenerator orderGenerator = TestScenario.GetOrderGenerator(productionDomainContext
                , new MinDeliveryTime(960)
                , new MaxDeliveryTime(1440)
                , new OrderArrivalRate(0.025));

            // start
            for (int i = 0; productionDomainContext.CustomerOrderParts.Count() < 10; i++)
            {
                // remove all DemandToProvider entries
                productionDomainContext.DemandToProviders.RemoveRange(productionDomainContext
                    .DemandToProviders);
                productionDomainContext.ProviderToDemand.RemoveRange(productionDomainContext
                    .ProviderToDemand);

                // init data structures
                IDbTransactionData dbTransactionData =
                    ZppConfiguration.CacheManager.ReloadTransactionData();

                ProcessDbDemands(dbTransactionData, dbTransactionData.T_CustomerOrderPartGetAll(),
                    0, withForwardScheduling);

                
                var simulationInterval = new SimulationInterval(0 * i, 1440 * i);
                ISimulator simulator = new Simulator(dbTransactionData);
                simulator.ProcessCurrentInterval(simulationInterval, orderGenerator);
                dbTransactionData.PersistDbCache();
            }
        }

        /**
         * - save providers
         * - save dependingDemands
         */
        public static void ProcessProvidingResponse(ResponseWithProviders responseWithProviders,
            IProviderManager providerManager, StockManager stockManager,
            IDbTransactionData dbTransactionData, Demand demand,
            IOpenDemandManager openDemandManager)
        {
            if (responseWithProviders == null)
            {
                return;
            }

            if (responseWithProviders.GetDemandToProviders() != null)
            {
                foreach (var demandToProvider in responseWithProviders.GetDemandToProviders())
                {
                    if (demandToProvider.GetDemandId().Equals(demand.GetId()) == false)
                    {
                        throw new MrpRunException(
                            "This demandToProvider does not fit to given demand.");
                    }

                    providerManager.AddDemandToProvider(demandToProvider);

                    if (responseWithProviders.GetProviders() != null)
                    {
                        Provider provider = responseWithProviders.GetProviders()
                            .GetProviderById(demandToProvider.GetProviderId());
                        if (provider != null)
                        {
                            stockManager.AdaptStock(provider, dbTransactionData, openDemandManager);
                            providerManager.AddProvider(responseWithProviders.GetDemandId(),
                                provider, demandToProvider.GetQuantity());
                        }
                    }
                }
            }
        }

        private static IDemands ProcessNextDemand(IDbTransactionData dbTransactionData,
            Demand demand, IProvidingManager orderManager,
            StockManager stockManager, IProviderManager providerManager,
            IOpenDemandManager openDemandManager)
        {
            ResponseWithProviders responseWithProviders;

            // SE:I --> satisfy by orders (PuOP/PrOBom)
            if (demand.GetType() == typeof(StockExchangeDemand))
            {
                responseWithProviders = orderManager.Satisfy(demand,
                    demand.GetQuantity(), dbTransactionData);

                ProcessProvidingResponse(responseWithProviders, providerManager, stockManager,
                    dbTransactionData, demand, openDemandManager);
            }
            // COP or PrOB --> satisfy by SE:W
            else
            {
                responseWithProviders = stockManager.Satisfy(demand,
                    demand.GetQuantity(), dbTransactionData);

                ProcessProvidingResponse(responseWithProviders, providerManager, stockManager,
                    dbTransactionData, demand, openDemandManager);
            }

            if (responseWithProviders.GetRemainingQuantity().IsNull() == false)
            {
                throw new MrpRunException(
                    $"'{demand}' was NOT satisfied: remaining is {responseWithProviders.GetRemainingQuantity()}");
            }

            return providerManager.GetNextDemands();
        }


        private static void ProcessDbDemands(IDbTransactionData dbTransactionData,
            IDemands dbDemands, int count,
            bool withForwardScheduling)
        {
            // init
            IDemands finalAllDemands = new Demands();
            int MAX_DEMANDS_IN_QUEUE = 100000;

            FastPriorityQueue<DemandQueueNode> demandQueue =
                new FastPriorityQueue<DemandQueueNode>(MAX_DEMANDS_IN_QUEUE);

            StockManager globalStockManager =
                new StockManager();

            StockManager stockManager = new StockManager(globalStockManager);
            IProviderManager providerManager = new ProviderManager(dbTransactionData);

            IProvidingManager orderManager = new OrderManager();

            IOpenDemandManager openDemandManager = new OpenDemandManager();

            foreach (var demand in dbDemands)
            {
                demandQueue.Enqueue(new DemandQueueNode(demand),
                    demand.GetDueTime(dbTransactionData).GetValue());
            }

            while (demandQueue.Count != 0)
            {
                DemandQueueNode firstDemandInQueue = demandQueue.Dequeue();

                IDemands nextDemands = ProcessNextDemand(dbTransactionData,
                    firstDemandInQueue.GetDemand(), orderManager, stockManager,
                    providerManager, openDemandManager);
                if (nextDemands != null)
                {
                    finalAllDemands.AddAll(nextDemands);
                    // TODO: EnqueueAll()
                    foreach (var demand in nextDemands)
                    {
                        demandQueue.Enqueue(new DemandQueueNode(demand),
                            demand.GetDueTime(dbTransactionData).GetValue());
                    }
                }
            }
            /*
            // forward scheduling
            DueTime minDueTime = ForwardScheduler.FindMinDueTime(finalAllDemands,
                providerManager.GetProviders(), dbTransactionData);
            if (minDueTime.GetValue() < 0)
            {
                T_CustomerOrderPart thisCustomerOrderPart =
                    (T_CustomerOrderPart) oneCustomerOrderPart.GetIDemand();
                thisCustomerOrderPart.CustomerOrder.DueTime += Math.Abs(minDueTime.GetValue());
                ProcessNextCustomerOrderPart(dbTransactionData, oneCustomerOrderPart,
                    globalStockManager);
                return;
            }
            */


            // forward scheduling
            // TODO: remove this once forward scheduling is implemented
            // TODO 2: in forward scheduling, min must be calculuted by demand & provider,
            // not only providers, since operations are on PrOBom (which are demands)
            if (withForwardScheduling)
            {
                int min = 0;
                foreach (var provider in providerManager.GetProviders())
                {
                    int start = provider.GetStartTime(dbTransactionData).GetValue();
                    if (start < min)
                    {
                        min = start;
                    }
                }


                if (min < 0)
                {
                    foreach (var dbDemand in dbDemands)
                    {
                        if (dbDemand.GetType() == typeof(CustomerOrderPart))
                        {
                            T_CustomerOrderPart customerOrderPart =
                                ((T_CustomerOrderPart) ((CustomerOrderPart) dbDemand).ToIDemand());
                            customerOrderPart.CustomerOrder.DueTime =
                                customerOrderPart.CustomerOrder.DueTime + Math.Abs(min);
                        }
                    }

                    ProcessDbDemands(dbTransactionData, dbDemands, count++,
                        withForwardScheduling);
                }
            }


            // persisting data
            if (count == 0)
                // it_s the first run, only do following here,
                // avoids executing this twice (else latest in forward scheduling recursion would also execute this)
            {
                // write data to dbTransactionData
                globalStockManager.AdaptStock(stockManager);
                dbTransactionData.DemandsAddAll(finalAllDemands);
                dbTransactionData.ProvidersAddAll(providerManager.GetProviders());
                dbTransactionData.SetProviderManager(providerManager);

                // job shop scheduling
                MachineManager machineManager = new MachineManager();
                machineManager.JobSchedulingWithGifflerThompsonAsZaepfel(dbTransactionData,
                    new PriorityRule());

                dbTransactionData.PersistDbCache();

                LOGGER.Info("MrpRun done.");
            }
        }
    }
}