using Akka.Actor;
using AkkaSim.Definitions;
using Master40.DB.DataModel;
using Zpp.Common.ProviderDomain.Wrappers;

namespace Zpp.Simulation.Agents.Resource
{
    partial class Resource
    {
        public enum Command
        {
            Ready
        }

        public class Work : SimulationMessage
        {
            public static Work Create(ProductionOrderOperation operation, IActorRef target)
            {
                return new Work(operation, target);
            }
            private Work(object message, IActorRef target) : base(message, target)
            { }
            public ProductionOrderOperation GetOperation => this.Message as ProductionOrderOperation;
        }
    
        
        public class FinishWork : SimulationMessage
        {
            public static FinishWork Create(ProductionOrderOperation operation, IActorRef target)
            {
                return new FinishWork(operation, target);
            }
            private FinishWork(object Message, IActorRef target) : base(Message, target)
            { }
            public ProductionOrderOperation GetOperation =>  this.Message as ProductionOrderOperation;
        }
    }
}
