using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using AkkaSim.Definitions;
using Zpp.OrderGraph;
using Zpp.Simulation.Agents.JobDistributor.Types;

namespace Zpp.Simulation.Agents.JobDistributor.Skills
{
    public class OperationsToDistribute : SimulationMessage
    {
        public static OperationsToDistribute Create(ProductionOrderToOperationGraph machine, IActorRef target)
        {
            return new OperationsToDistribute(machine, target);
        }
        private OperationsToDistribute(object message, IActorRef target) : base(message, target)
        {
        }
        public ProductionOrderToOperationGraph GetProductionOrderToOperations => this.Message as ProductionOrderToOperationGraph;
    }
}
