using System.Collections.Generic;
using Akka.Actor;
using AkkaSim.Definitions;
using Master40.DB.DataModel;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Mrp.MachineManagement;
using Zpp.Simulation.Agents.JobDistributor.Types;

namespace Zpp.Simulation.Agents.JobDistributor
{
    partial class JobDistributor
    {
        public enum Command
        {
            GetWork
        }
        public class OperationsToDistibute : SimulationMessage
        {
            public static OperationsToDistibute Create(OperationManager machine, IActorRef target)
            {
                return new OperationsToDistibute(machine, target);
            }
            private OperationsToDistibute(object message, IActorRef target) : base(message, target)
            {
            }
            public OperationManager GetOperations => this.Message as OperationManager;
        }

        public class ProductionOrderFinished : SimulationMessage
        {
            public static ProductionOrderFinished Create(ProductionOrderOperation operation, IActorRef target)
            {
                return new ProductionOrderFinished(operation, target);
            }
            public ProductionOrderFinished(object message, IActorRef target) : base(message, target)
            {
            }
            public ProductionOrderOperation GetOperation => this.Message  as ProductionOrderOperation;
        }

        public class AddResources : SimulationMessage
        {
            public static AddResources Create(ResourceDictionary machines, IActorRef target)
            {
                return new AddResources(machines, target);
            }
            private AddResources(object message, IActorRef target) : base(message, target)
            {
            }
            public ResourceDictionary GetMachines => this.Message as ResourceDictionary;
        }
    }
}
