using System;
using System.Data;
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
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.NodeManagement;
using Zpp.Mrp.Scheduling;
using Zpp.Mrp.StockManagement;
using Zpp.Simulation;
using Zpp.Simulation.Types;
using Zpp.Test.Configuration.Scenarios;
using Zpp.Utils;
using Zpp.Utils.Queue;
using Zpp.WrappersForCollections;

namespace Zpp.Mrp
{
    public class MrpRun : IMrpRun
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly JobShopScheduler _jobShopScheduler = new JobShopScheduler();

        private readonly IOrderManager _orderManager = new OrderManager();
        private readonly OrderGenerator _orderGenerator;

        public MrpRun()
        {
            ProductionDomainContext productionDomainContext =
                ZppConfiguration.CacheManager.GetProductionDomainContext();

            _orderGenerator = TestScenario.GetOrderGenerator(productionDomainContext,
                new MinDeliveryTime(200), new MaxDeliveryTime(1440), new OrderArrivalRate(0.025));
        }

        /**
         * Only at start the demands are customerOrders
         */
        public void Start()
        {
            // _productionDomainContext
            for (int i = 0; i < 2; i++)
            {
                // init transactionData
                IDbTransactionData dbTransactionData =
                    ZppConfiguration.CacheManager.ReloadTransactionData();

                // execute mrp2
                ManufacturingResourcePlanning(dbTransactionData.T_CustomerOrderPartGetAll());

                int interval = 1440;
                var simulationInterval = new SimulationInterval(i * interval, interval * (i + 1));

                CreateConfirmations(simulationInterval);

                ApplyConfirmations();
            }
        }

        /**
         * - save providers
         * - save dependingDemands
         */
        private void ProcessProvidingResponse(ResponseWithProviders responseWithProviders,
            IStockManager stockManager, Demand demand,
            EntityCollector entityCollector)
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

                    entityCollector._demandToProviderTable.Add(demandToProvider);

                    if (responseWithProviders.GetProviders() != null)
                    {
                        Provider provider = responseWithProviders.GetProviders()
                            .GetProviderById(demandToProvider.GetProviderId());
                        if (provider != null)
                        {
                            EntityCollector dependings = stockManager.AdaptStock(provider);
                            entityCollector._providers.Add(provider);
                            entityCollector.AddAll(dependings);
                        }
                    }
                }
            }
        }

        public EntityCollector MaterialRequirementsPlanning(Demand demand,
            IStockManager stockManager)
        {
            EntityCollector entityCollector =
                new EntityCollector();
            ResponseWithProviders responseWithProviders;

            // SE:I --> satisfy by orders (PuOP/PrOBom)
            if (demand.GetType() == typeof(StockExchangeDemand))
            {
                responseWithProviders = _orderManager.Satisfy(demand, demand.GetQuantity());

                ProcessProvidingResponse(responseWithProviders, stockManager, demand,
                    entityCollector);
            }
            // COP or PrOB --> satisfy by SE:W
            else
            {
                responseWithProviders = stockManager.Satisfy(demand, demand.GetQuantity());

                ProcessProvidingResponse(responseWithProviders, stockManager, demand,
                    entityCollector);
            }

            if (responseWithProviders.GetRemainingQuantity().IsNull() == false)
            {
                throw new MrpRunException(
                    $"'{demand}' was NOT satisfied: remaining is {responseWithProviders.GetRemainingQuantity()}");
            }

            return entityCollector;
        }


        public void ManufacturingResourcePlanning(IDemands dbDemands)
        {
            if (dbDemands == null || dbDemands.Any() == false)
            {
                return;
            }

            // init
            int MAX_DEMANDS_IN_QUEUE = 100000;

            FastPriorityQueue<DemandQueueNode> demandQueue =
                new FastPriorityQueue<DemandQueueNode>(MAX_DEMANDS_IN_QUEUE);

            IStockManager stockManager = new StockManager();

            foreach (var demand in dbDemands)
            {
                demandQueue.Enqueue(new DemandQueueNode(demand), demand.GetDueTime().GetValue());
            }

            // MaterialRequirementsPlanning
            EntityCollector allCreatedEntities = new EntityCollector();
            while (demandQueue.Count != 0)
            {
                DemandQueueNode firstDemandInQueue = demandQueue.Dequeue();

                EntityCollector entity =
                    MaterialRequirementsPlanning(firstDemandInQueue.GetDemand(), stockManager);
                allCreatedEntities.AddAll(entity);

                // TODO: EnqueueAll()
                foreach (var demand in entity._demands)
                {
                    demandQueue.Enqueue(new DemandQueueNode(demand),
                        demand.GetDueTime().GetValue());
                }
            }
            // write data to _dbTransactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            dbTransactionData.AddAll(allCreatedEntities);
            // End of MaterialRequirementsPlanning

            // TODO: remove this line (debugging only)
            dbTransactionData.PersistDbCache();
            
            // forward scheduling
            ScheduleForward();

            // job shop scheduling
            JobShopScheduling();

            // persisting cached/created data
            dbTransactionData.PersistDbCache();

            Logger.Info("MrpRun done.");
        }

        public void ScheduleBackward()
        {
            throw new NotImplementedException();
        }

        public void ScheduleForward()
        {
            IForwardScheduler forwardScheduler = new ForwardScheduler();
            forwardScheduler.ScheduleForward();
        }

        public void JobShopScheduling()
        {
            _jobShopScheduler.ScheduleWithGifflerThompsonAsZaepfel(new PriorityRule());
        }

        public void CreateConfirmations(SimulationInterval simulationInterval)
        {
            ISimulator simulator = new Simulator();
            simulator.ProcessCurrentInterval(simulationInterval, _orderGenerator);
            ZppConfiguration.CacheManager.GetDbTransactionData().PersistDbCache();
        }

        public void ApplyConfirmations()
        {
            /**
             * - löschen aller Verbindungen zwischen P(SE:W) und D(SE:I)
             * - PrO: D(SE:I) bis P(SE:W) erhalten wenn eine der Ops angefangen
             */


            // _dbTransactionData.PersistDbCache();
        }
    }
}