using System.Collections.Generic;
using Akka.Actor;
using AkkaSim.Definitions;
using Master40.DB.DataModel;

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
            public static OperationsToDistibute Create(List<T_ProductionOrderOperation> machine, IActorRef target)
            {
                return new OperationsToDistibute(machine, target);
            }
            private OperationsToDistibute(object message, IActorRef target) : base(message, target)
            {
            }
            public List<T_ProductionOrderOperation> GetOperations => this.Message as List<T_ProductionOrderOperation>;
        }

        public class ProductionOrderFinished : SimulationMessage
        {
            public static ProductionOrderFinished Create(T_ProductionOrderOperation operation, IActorRef target)
            {
                return new ProductionOrderFinished(operation, target);
            }
            public ProductionOrderFinished(object message, IActorRef target) : base(message, target)
            {
            }
            public T_ProductionOrderOperation GetOperation => this.Message  as T_ProductionOrderOperation;
        }

        public class AddMachine : SimulationMessage
        {
            public static AddMachine Create(M_Machine machine, IActorRef target)
            {
                return new AddMachine(machine, target);
            }
            private AddMachine(object message, IActorRef target) : base(message, target)
            {
            }
            public M_Machine GetMachine => this.Message as M_Machine;
        }
    }
}
