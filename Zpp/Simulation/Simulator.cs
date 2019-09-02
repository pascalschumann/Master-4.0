using System;
using Akka.Actor;
using AkkaSim.Definitions;
using Master40.DB.Enums;
using System.Diagnostics;
using System.Threading;
using Zpp.DbCache;
using Zpp.Simulation.Agents.JobDistributor;
using Zpp.Simulation.Agents.JobDistributor.Types;
using Zpp.Simulation.Monitors;
using Zpp.Simulation.Types;

namespace Zpp.Simulation
{
    public class Simulator
    {
        private readonly IDbMasterDataCache _dbMasterDataCache;
        private readonly IDbTransactionData _dbTransactionData;
        private long _currentTime { get; set; } = 0;
        private SimulationConfig _simulationConfig { get; }
        private AkkaSim.Simulation _akkaSimulation { get; set; }
        public Simulator()
        {
            _simulationConfig = new SimulationConfig(false, 1440);
        }


        public bool ProcessCurrentInterval(SimulationInterval simulationInterval, IDbMasterDataCache dbMasterDataCache, IDbTransactionData dbTransactionData)
        {
            Debug.WriteLine("Start simulation system. . . ");

            _currentTime = simulationInterval.StartAt;
            _akkaSimulation = new AkkaSim.Simulation(_simulationConfig);

            var jobDistributor = _akkaSimulation.ActorSystem.ActorOf(JobDistributor.Props(_akkaSimulation.SimulationContext, _currentTime), "JobDistributor");
            // Create a Machines
            CreateResource(jobDistributor, _akkaSimulation);
            
            // Set purchased Demands finished.
            ProvideRequiredPurchaseForThisInterval(simulationInterval);
            
            // Distribute Ready Jobs
            ProvideJobDistributor(simulationInterval, jobDistributor);

            // TODO What to do with finished Jobs? How is PrO connected to the StockExchange. 
            /// a. Provide a Stockexchange Key with every ProductionOrder to complete SE.
            /// b. delete all Stockexchanges that are "ToProduce"
            /// --> _c. satisfy the first not yet satisfied Stockexchange 
            // Handle JobFinish
            


            // Handle Simulation End

            // 

            // for (int i = 0; i < 3000; i++)
            // {
            //     var materialRequest = new MaterialRequest(CreateBOM(), new Dictionary<int, bool>(), 0, r.Next(50, 500), true);
            //     var request = new JobDistributor.ProductionOrder(materialRequest, jobDistributor);
            //     sim.SimulationContext.Tell(request, null);
            // }

            // example to monitor for FinishWork Messages.

            var monitor = _akkaSimulation.ActorSystem
                                         .ActorOf(props: WorkTimeMonitor.Props(time: 0),
                                                   name: "SimulationMonitor");
            if(_akkaSimulation.IsReady())
            {
                _akkaSimulation.RunAsync();
                Continuation(_simulationConfig.Inbox, _akkaSimulation);
            }



            Debug.WriteLine("System shutdown. . . ");
            Debug.WriteLine("System Runtime " + _akkaSimulation.ActorSystem.Uptime);
            return true;
        }

        private void ProvideJobDistributor(SimulationInterval simulationInterval, IActorRef jobDistributor)
        {
            // var productionOrderOperations = _dBcontext.ProductionOrderOperations
            //                                         .Include(x => x.Machine)
            //                                         .Include(x => x.ProductionOrderBoms)
            //                                           .ThenInclude(x => x.ArticleChild)
            //                                         .Include(x => x.ProductionOrderBoms)
            //                                           .ThenInclude(x => x.ProductionOrderParent)
            //                                         .Where(x => x.Start < simulationInterval.EndAt
            //                                                  && x.ProducingState == ProducingState.Created)
            //                                         .ToList();
            // ;
            // 
            // _akkaSimulation.SimulationContext.Tell(JobDistributor.OperationsToDistibute.Create(productionOrderOperations, jobDistributor), ActorRefs.NoSender);
        }

        /// <summary>
        /// can be done by some sort of StockManager later if time periods not static
        /// </summary>
        /// <param name="simulationInterval"></param>
        private void ProvideRequiredPurchaseForThisInterval(SimulationInterval simulationInterval)
        {
            var stockExchanges = _dbTransactionData.GetAggregator().GetProviderForCurrent(simulationInterval); 
                // .GetAll StockExchangeProvidersGetAll().GetAll();
                foreach (var stockExchange in stockExchanges)
                {
                    stockExchange.SetProvided(stockExchange.GetDueTime());
                }
        }

        private void CreateResource(IActorRef jobDistributor, AkkaSim.Simulation sim)
        {
            var machines = ResourceManager.GetResources(_dbMasterDataCache);
            var createMachines = JobDistributor.AddMachines.Create(machines, jobDistributor);
            sim.SimulationContext.Tell(createMachines, ActorRefs.Nobody);
        }

        private static void Continuation(Inbox inbox, AkkaSim.Simulation sim)
        {

            var something = inbox.ReceiveAsync().Result;
            switch (something)
            {
                case SimulationMessage.SimulationState.Started:
                    Continuation(inbox, sim);
                    break;
                case SimulationMessage.SimulationState.Stopped:
                    sim.Continue();
                    Continuation(inbox, sim);
                    break;
                case SimulationMessage.SimulationState.Finished:
                    sim.ActorSystem.Terminate();
                    break;
                default:
                    break;
            }
        }
    }
}
