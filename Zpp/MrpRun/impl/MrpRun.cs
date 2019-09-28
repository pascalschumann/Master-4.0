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
        private readonly OrderGenerator _orderGenerator;

        public MrpRun()
        {
            ProductionDomainContext productionDomainContext =
                ZppConfiguration.CacheManager.GetProductionDomainContext();

            _orderGenerator = TestScenario.GetOrderGenerator(productionDomainContext,
                new MinDeliveryTime(200), new MaxDeliveryTime(1440), new OrderArrivalRate(0.0001));
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

                int interval = 1440;
                var simulationInterval = new SimulationInterval(i * interval, interval * (i + 1));
                CreateOrders(simulationInterval);
                
                // execute mrp2
                ManufacturingResourcePlanning(dbTransactionData.T_CustomerOrderPartGetAll());

                CreateConfirmations(simulationInterval);

                ApplyConfirmations();
            }
        }


        public EntityCollector MaterialRequirementsPlanning(Demand demand,
            IProviderManager providerManager)
        {
            EntityCollector entityCollector = new EntityCollector();

            EntityCollector response = providerManager.Satisfy(demand, demand.GetQuantity());
            entityCollector.AddAll(response);
            providerManager.AdaptStock(response.GetProviders());
            response = providerManager.CreateDependingDemands(entityCollector.GetProviders());
            entityCollector.AddAll(response);
            
            if (entityCollector.IsSatisfied(demand) == false)
            {
                throw new MrpRunException($"'{demand}' was NOT satisfied: remaining is " +
                                          $"{entityCollector.GetRemainingQuantity(demand)}");
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

            IProviderManager providerManager = new ProviderManager();

            foreach (var demand in dbDemands)
            {
                demandQueue.Enqueue(new DemandQueueNode(demand), demand.GetDueTime().GetValue());
            }

            // MaterialRequirementsPlanning
            EntityCollector allCreatedEntities = new EntityCollector();
            while (demandQueue.Count != 0)
            {
                DemandQueueNode firstDemandInQueue = demandQueue.Dequeue();

                EntityCollector response =
                    MaterialRequirementsPlanning(firstDemandInQueue.GetDemand(), providerManager);
                allCreatedEntities.AddAll(response);

                // TODO: EnqueueAll()
                foreach (var demand in response.GetDemands())
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

            // forward scheduling
            // ScheduleForward();

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
        
        public void CreateOrders(SimulationInterval interval)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            var creationTime = interval.StartAt;
            var endOrderCreation = interval.EndAt;

            while (creationTime < endOrderCreation)
            {
                var order = _orderGenerator.GetNewRandomOrder(time: creationTime);
                foreach (var orderPart in order.CustomerOrderParts)
                {
                    dbTransactionData.CustomerOrderPartAdd(orderPart);
                }
                dbTransactionData.T_CustomerOrderGetAll().Add(order);
                // TODO : Handle this another way
                creationTime += order.CreationTime;
            }
            ZppConfiguration.CacheManager.GetDbTransactionData().PersistDbCache();
        }
    }
}