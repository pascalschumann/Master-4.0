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
using Zpp.Common.ProviderDomain.WrappersForCollections;
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
    public class MrpRun: IMrpRun
    {
        private static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();
        private readonly ProductionDomainContext _productionDomainContext;
        private IDbTransactionData _dbTransactionData;
        private readonly JobShopScheduler _jobShopScheduler = new JobShopScheduler();

        private readonly IOrderManager _orderManager = new OrderManager();
        
        private readonly Demands _newCreatedDemands = new Demands();
        private Provider _newCreatedProvider;

        private readonly OrderGenerator _orderGenerator;

        public MrpRun(ProductionDomainContext productionDomainContext)
        {
            _productionDomainContext = productionDomainContext;
            
            _orderGenerator = TestScenario.GetOrderGenerator(_productionDomainContext
                , new MinDeliveryTime(960)
                , new MaxDeliveryTime(1440)
                , new OrderArrivalRate(0.025));
        }

        /**
         * Only at start the demands are customerOrders
         */
        public void Start(bool withForwardScheduling = true)
        {
            // _productionDomainContext
            for (int i = 0; _productionDomainContext.CustomerOrderParts.Count() < 10; i++)
            {
                ApplyConfirmations();

                // init transactionData
                _dbTransactionData =
                    ZppConfiguration.CacheManager.ReloadTransactionData();

                // execute mrp2
                ManufacturingResourcePlanning(_dbTransactionData.T_CustomerOrderPartGetAll(),
                    0, withForwardScheduling);

                var simulationInterval = new SimulationInterval(0 * i, 1440 * i);
                
                CreateConfirmations(simulationInterval);
            }
        }

        /**
         * - save providers
         * - save dependingDemands
         */
        private void ProcessProvidingResponse(ResponseWithProviders responseWithProviders,
            IStockManager stockManager,
            Demand demand)
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

                    _dbTransactionData.DemandToProviderAdd(demandToProvider);

                    if (responseWithProviders.GetProviders() != null)
                    {
                        Provider provider = responseWithProviders.GetProviders()
                            .GetProviderById(demandToProvider.GetProviderId());
                        if (provider != null)
                        {
                            stockManager.AdaptStock(provider, _dbTransactionData);
                            _newCreatedProvider = provider;

                            Demands dependingDemands = provider.GetAllDependingDemands();
                            if (dependingDemands != null && dependingDemands.Any())
                            {
                                _newCreatedDemands.AddAll(dependingDemands);    
                            }
                            
                        }
                        
                    }
                }
            }
        }

        public void MaterialRequirementsPlanning(Demand demand, IStockManager stockManager)
        {
            ResponseWithProviders responseWithProviders;

            // SE:I --> satisfy by orders (PuOP/PrOBom)
            if (demand.GetType() == typeof(StockExchangeDemand))
            {
                responseWithProviders = _orderManager.Satisfy(demand,
                    demand.GetQuantity());

                ProcessProvidingResponse(responseWithProviders, stockManager,
                    demand);
            }
            // COP or PrOB --> satisfy by SE:W
            else
            {
                responseWithProviders = stockManager.Satisfy(demand,
                    demand.GetQuantity());

                ProcessProvidingResponse(responseWithProviders, stockManager,
                    demand);
            }

            if (responseWithProviders.GetRemainingQuantity().IsNull() == false)
            {
                throw new MrpRunException(
                    $"'{demand}' was NOT satisfied: remaining is {responseWithProviders.GetRemainingQuantity()}");
            }
        }


        public void ManufacturingResourcePlanning(IDemands dbDemands, int count,
            bool withForwardScheduling)
        {
            // init
            IDemands finalAllDemands = new Demands();
            IProviders finalAllProviders = new Providers();
            int MAX_DEMANDS_IN_QUEUE = 100000;

            FastPriorityQueue<DemandQueueNode> demandQueue =
                new FastPriorityQueue<DemandQueueNode>(MAX_DEMANDS_IN_QUEUE);

            StockManager globalStockManager =
                new StockManager();

            IStockManager stockManager = new StockManager(globalStockManager);

            foreach (var demand in dbDemands)
            {
                demandQueue.Enqueue(new DemandQueueNode(demand),
                    demand.GetDueTime(_dbTransactionData).GetValue());
            }

            while (demandQueue.Count != 0)
            {
                DemandQueueNode firstDemandInQueue = demandQueue.Dequeue();

                MaterialRequirementsPlanning(firstDemandInQueue.GetDemand(), stockManager);
                finalAllProviders.Add(_newCreatedProvider);
                if (_newCreatedDemands.Any())
                {
                    finalAllDemands.AddAll(_newCreatedDemands);
                    // TODO: EnqueueAll()
                    foreach (var demand in _newCreatedDemands)
                    {
                        demandQueue.Enqueue(new DemandQueueNode(demand),
                            demand.GetDueTime(_dbTransactionData).GetValue());
                    }
                    _newCreatedDemands.Clear();
                }
            }

            // forward scheduling
            if (withForwardScheduling)
            {
                ScheduleForward(count);
            }


            // persisting data
            if (count == 0)
                // it_s the first run, only do following here,
                // avoids executing this twice (else latest in forward scheduling recursion would also execute this)
            {
                // write data to dbTransactionData
                globalStockManager.AdaptStock(stockManager);
                _dbTransactionData.DemandsAddAll(finalAllDemands);
                _dbTransactionData.ProvidersAddAll(finalAllProviders);

                // job shop scheduling
                JobShopScheduling();

                _dbTransactionData.PersistDbCache();

                LOGGER.Info("MrpRun done.");
            }
            
            
        }

        public void ScheduleBackward()
        {
            throw new NotImplementedException();
        }

        public void ScheduleForward(int count)
        {
            Demands demands = _dbTransactionData.T_CustomerOrderPartGetAll();
            
            // TODO: remove this once forward scheduling is implemented
            // TODO 2: in forward scheduling, min must be calculuted by demand & provider,
            // not only providers, since operations are on PrOBom (which are demands)
            int min = 0;
            foreach (var provider in _dbTransactionData.ProvidersGetAll())
            {
                int start = provider.GetStartTime(_dbTransactionData).GetValue();
                if (start < min)
                {
                    min = start;
                }
            }


            if (min < 0)
            {
                foreach (var demand in demands)
                {
                    if (demand.GetType() == typeof(CustomerOrderPart))
                    {
                        T_CustomerOrderPart customerOrderPart =
                            ((T_CustomerOrderPart)  demand.ToIDemand());
                        customerOrderPart.CustomerOrder.DueTime =
                            customerOrderPart.CustomerOrder.DueTime + Math.Abs(min);
                    }
                }

                ManufacturingResourcePlanning(demands, count+1,
                    true);
            }
        }

        public void JobShopScheduling()
        {
            _jobShopScheduler.JobSchedulingWithGifflerThompsonAsZaepfel(
                new PriorityRule());
        }

        public void CreateConfirmations( SimulationInterval simulationInterval)
        {
            ISimulator simulator = new Simulator(_dbTransactionData);
            simulator.ProcessCurrentInterval(simulationInterval, _orderGenerator);
            _dbTransactionData.PersistDbCache();
        }

        public void ApplyConfirmations()
        {
            // TODO: This is not correct an incomplete
            // remove all DemandToProvider entries
            _productionDomainContext.DemandToProviders.RemoveRange(_productionDomainContext
                .DemandToProviders);
            _productionDomainContext.ProviderToDemand.RemoveRange(_productionDomainContext
                .ProviderToDemand);
        }
    }
}