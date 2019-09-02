using Akka.Actor;
using AkkaSim;
using AkkaSim.Definitions;
using Master40.DB.DataModel;
using Master40.SimulationCore.Helper;
using System;
using System.Collections.Generic;
using Zpp.Simulation.Agents.JobDistributor.Types;

namespace Zpp.Simulation.Agents.JobDistributor
{
    partial class JobDistributor : SimulationElement
    {
        public ResourceManager ResourceManager { get; } = new ResourceManager();

        public static Props Props(IActorRef simulationContext, long time)
        {
            return Akka.Actor.Props.Create(() => new JobDistributor(simulationContext, time));
        }
        public JobDistributor(IActorRef simulationContext, long time) 
            : base(simulationContext, time)
        {
        }

        protected override void Do(object o)
        {
            switch (o)
            {
                // case AddMachine m: CreateMachines(m.GetMachine, TimePeriod); break;
                case OperationsToDistibute m  : InitializeDistribution(m.GetOperations); break;
                case Command.GetWork    : PushWorkToResource(Sender); break;
                case ProductionOrderFinished m: ProvideMaterial(m.GetOperation); break;
                default: new Exception("Message type could not be handled by SimulationElement"); break;
            }
        }

        private void InitializeDistribution(List<T_ProductionOrderOperation> operations)
        {
            // ResourceManager.AddOperationQueue(operations);
            // TODO Check is Item is in Stock ? 

            // Start Work
            // var machineRefs = ResourceManager.GetMachineRefs();
            // foreach (var machineRef in machineRefs)
            // {
            //     PushWorkToResource(machineRef);
            // }

        }


        private void PushWorkToResource(IActorRef machineRef)
        {
            // var operation = ResourceManager.NextElementFor(machineRef);
            // if (operation == null) return;
            // var msg = Resource.Resource.Work.Create(operation, machineRef);
            // 
            // // Operation is on Time or Delayed
            // if (operation.Start <= TimePeriod)
            // {
            //     _SimulationContext.Tell(msg, this.Self);
            //     return;
            // } // else operation starts in the future and has to wait.
            // Schedule(operation.Start - TimePeriod, msg);
        }

        private void CreateMachines(M_Machine machine, long time)
        {
            var machineNumber = ResourceManager.Count + 1;
            var agentName = $"{machine.Name}({machineNumber})".ToActorName();
            var resourceRef = Context.ActorOf(Resource.Resource.Props(_SimulationContext, time)
                                                                , agentName);
            var resource = new ResourceDetails(machine, resourceRef);
            ResourceManager.AddResource(resource);

        }

        private void ProvideMaterial(T_ProductionOrderOperation o)
        {
            PushWorkToResource(Sender);
            // TODO Check for Preconditions (Previous job is finished and Material is Provided.)
          //  var po = o as ProductionOrderFinished;
          //  var request = po.Message as MaterialRequest;
          //  if (request.Material.Name == "Table")
          //      Console.WriteLine("Table No: "+ ++MaterialCounter);
          //  //Console.WriteLine("Time: " + TimePeriod + " Number " + MaterialCounter + " Finished: " + request.Material.Name);
          //  if (!request.IsHead)
          //  {
          //      var parrent = WaitingItems.Single(x => x.Id == request.Parrent);
          //      parrent.ChildRequests[request.Id] = true;
          //      
          //      // now check if item can be deployd to ReadyQueue
          //      if (parrent.ChildRequests.All(x => x.Value == true))
          //      {
          //          WaitingItems.Remove(parrent);
          //          ReadyItems.Enqueue(parrent);
          //      }
          //  }
          //  Machines.Remove(Sender);
          //  Machines.Add(Sender, true);
          //
          //  
          //  PushWork();
        }

        protected override void Finish()
        {
            Console.WriteLine(Sender.Path + " has been Killed");
        }
    }
}
