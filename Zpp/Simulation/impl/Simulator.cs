using Akka.Actor;
using AkkaSim.Definitions;
using System.Diagnostics;
using System.Linq;
using Master40.DB.Enums;
using NLog.Targets;
using Zpp.DbCache;
using Zpp.OrderGraph;
using Zpp.Simulation.Agents.JobDistributor;
using Zpp.Simulation.Agents.JobDistributor.Skills;
using Zpp.Simulation.Agents.JobDistributor.Types;
using Zpp.Simulation.Monitors;
using Zpp.Simulation.Types;
using Zpp.WrappersForPrimitives;
using Master40.SimulationCore.DistributionProvider;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Configuration;

namespace Zpp.Simulation
{
    public class Simulator: ISimulator
    {
        private readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
        private long _currentTime { get; set; } = 0;
        private SimulationConfig _simulationConfig { get; }
        private AkkaSim.Simulation _akkaSimulation { get; set; }
        public SimulationInterval _simulationInterval { get; private set; }

        public Simulator()
        {
            _simulationConfig = new SimulationConfig(false, 300);

        }

        public void RunSimulationFor(OrderGenerator orderGenerator, SimulationInterval simulationInterval)
        {
            ProcessCurrentInterval(simulationInterval, orderGenerator);
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            dbTransactionData.PersistDbCache();
        }

        public bool ProcessCurrentInterval(SimulationInterval simulationInterval, OrderGenerator orderGenerator)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            Debug.WriteLine("Start simulation system. . . ");
            _simulationInterval = simulationInterval;

            _currentTime = simulationInterval.StartAt;
            _akkaSimulation = new AkkaSim.Simulation(_simulationConfig);
            var jobDistributor = _akkaSimulation.ActorSystem
                                                .ActorOf(props: JobDistributor.Props(_akkaSimulation.SimulationContext, _currentTime)
                                                        , name: "JobDistributor");

            // ToDo reflect CurrentTimespawn ?
            _akkaSimulation.Shutdown(simulationInterval.EndAt);
            // Create a Machines
            CreateResource(jobDistributor);
            
            // Set purchased Demands finished.
            ProvideRequiredPurchaseForThisInterval(simulationInterval);
            
            // Distribute Ready Jobs
            ProvideJobDistributor(jobDistributor);

            // TODO What to do with finished Jobs? How is PrO connected to the StockExchange. 
            /// a. Provide a Stockexchange Key with every ProductionOrder to complete SE.
            /// b. delete all Stockexchanges that are "ToProduce"
            /// --> _c. satisfy the first not yet satisfied Stockexchange 
            // Handle JobFinish

            var monitor = _akkaSimulation.ActorSystem
                                         .ActorOf(props: WorkTimeMonitor.Props(time: _currentTime),
                                                   name: "SimulationMonitor");
            if(_akkaSimulation.IsReady())
            {
                _akkaSimulation.RunAsync();
                Continuation(_simulationConfig.Inbox, _akkaSimulation);
            }

            Debug.WriteLine("Create new Orders");
            CreateOrders(orderGenerator, simulationInterval);

            Debug.WriteLine("System shutdown. . . ");
            Debug.WriteLine("System Runtime " + _akkaSimulation.ActorSystem.Uptime);
            return true;
        }

        private void CreateOrders(OrderGenerator orderGenerator, SimulationInterval interval)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            var creationTime = interval.StartAt;
            var endOrderCreation = interval.EndAt;

            while (creationTime < endOrderCreation)
            {
                var order = orderGenerator.GetNewRandomOrder(time: creationTime);
                foreach (var orderPart in order.CustomerOrderParts)
                {
                    dbTransactionData.CustomerOrderPartAdd(orderPart);
                }
                dbTransactionData.T_CustomerOrderGetAll().Add(order);
                // TODO : Handle this another way
                creationTime += order.CreationTime;
            }
        }

        private void ProvideJobDistributor(IActorRef jobDistributor)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            var operationManager = new ProductionOrderToOperationGraph();
            _akkaSimulation.SimulationContext
                           .Tell(message: OperationsToDistribute.Create(operationManager, jobDistributor)
                                ,sender: ActorRefs.NoSender);
        }

        /// <summary>
        /// can be done by some sort of StockManager later if time periods not static
        /// </summary>
        /// <param name="simulationInterval"></param>
        private void ProvideRequiredPurchaseForThisInterval(SimulationInterval simulationInterval)
        {
            var from = new DueTime((int)simulationInterval.StartAt);
            var to = new DueTime((int)simulationInterval.EndAt);
            var stockExchanges = ZppConfiguration.CacheManager.GetAggregator().GetProvidersForInterval(from, to);
            foreach (var stockExchange in stockExchanges)
            {
                stockExchange.SetProvided(stockExchange.GetDueTime());
            }
        }

        private void CreateResource(IActorRef jobDistributor)
        {
            var machines = ResourceManager.GetResources(_dbMasterDataCache);
            var createMachines = AddResources.Create(machines, jobDistributor);
            _akkaSimulation.SimulationContext.Tell(createMachines, ActorRefs.Nobody);
        }

        // TODO: replace --> with Simulator.Continuation(Inbox inbox, AkkaSim.Simulation sim);
        private static void Continuation(Inbox inbox, AkkaSim.Simulation sim)
        {
            var something = inbox.ReceiveAsync().Result;
            switch (something)
            {
                case SimulationMessage.SimulationState.Started:
                    Debug.WriteLine($"Simulation Start", "AKKA");
                    Continuation(inbox, sim);
                    break;
                case SimulationMessage.SimulationState.Stopped:
                    Debug.WriteLine($"Simulation Stop.", "AKKA");
                    sim.Continue();
                    Continuation(inbox, sim);
                    break;
                case SimulationMessage.SimulationState.Finished:
                    Debug.WriteLine($"Simulation Finish.", "AKKA");
                    sim.ActorSystem.Terminate();
                    break;
                default:
                    break;
            }
        }
    }
}
