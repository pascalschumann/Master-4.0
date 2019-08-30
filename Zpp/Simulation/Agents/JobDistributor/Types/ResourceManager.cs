using Akka.Actor;
using Master40.DB.DataModel;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Enums;

namespace Zpp.Simulation.Agents.JobDistributor.Types
{
    public class ResourceManager
    {
        private readonly Dictionary<IActorRef, OperationManager> _resources = new Dictionary<IActorRef, OperationManager>();

        public bool AddResource(OperationManager resource)
        {
            return _resources.TryAdd(resource.ResourceRef, resource);
        }

        public void AddOperationQueue(List<T_ProductionOrderOperation> operations)
        { 
            var operationsByMachine = operations.GroupBy(x => x.Machine
                                                        , x => x
                                                        , (machine, operationsForThisMachine) 
                                                            => new { Key = machine
                                                                    , operationsForThisMachine });

            foreach (var item in operationsByMachine)
            {
                var resource = _resources.Values.Single(x => x.Machine.Id == item.Key.Id);
                resource.JobQueue = new Queue<T_ProductionOrderOperation>(item.operationsForThisMachine);
                resource.SetStatusForFirstItemInQueue(ProducingState.Waiting);
            }
        }

        public List<IActorRef> GetMachineRefs()
        {
            return _resources.Select(x => x.Key).ToList();
        }

        public int Count => _resources.Count;

        public T_ProductionOrderOperation NextElementFor(IActorRef machineRef)
        {
            _resources.TryGetValue(machineRef, out OperationManager operationManager);
            
            if (operationManager != null && operationManager.HasJobs())
            {
                var operation = operationManager.JobQueue.Dequeue();
                operationManager.SetStatusForFirstItemInQueue(ProducingState.Waiting);
                return operation;
            }
            return null;
        }
    }
}