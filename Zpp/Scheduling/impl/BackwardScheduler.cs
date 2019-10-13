using System.Linq;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.StackSet;

namespace Zpp.Scheduling.impl
{
    public class BackwardScheduler : IBackwardsScheduler
    {
        private readonly IStackSet<INode> _S;
        private readonly IDirectedGraph<INode> _orderOperationGraph;
        private readonly bool _clearOldTimes;

        public BackwardScheduler(IStackSet<INode> S, IDirectedGraph<INode> orderOperationGraph,
            bool clearOldTimes)
        {
            _S = S;
            _orderOperationGraph = orderOperationGraph;
            _clearOldTimes = clearOldTimes;
        }

        public void ScheduleBackward()
        {
            // S = {0} (alle einplanbaren "Operation"=Demand/Provider Elemente)

            if (_clearOldTimes)
            {
                // d_0 = 0
                foreach (var uniqueNode in _orderOperationGraph.GetAllUniqueNodes())
                {
                    if (uniqueNode.GetEntity().GetType() != typeof(CustomerOrderPart))
                    {
                        uniqueNode.GetEntity().ClearStartTime();
                        uniqueNode.GetEntity().ClearEndTime();
                    }
                }
            }


            // while S nor empty do
            while (_S.Any())
            {
                INode i = _S.PopAny();
                IScheduleNode iAsScheduleNode = i.GetEntity();

                INodes successorNodes = _orderOperationGraph.GetSuccessorNodes(i);
                if (successorNodes != null && successorNodes.Any())
                {
                    foreach (var successor in successorNodes)
                    {
                        IScheduleNode successorScheduleNode = successor.GetEntity();
                        
                        // TODO: Konservativ vorwärtsterminieren ist korrekt,
                        // aber rückwärts muss wenn immer möglich terminiert werden
                        // (prüfe parents und ermittle minStart und setze das)
                        
                        // if successor starts before endTime of current d/p --> change that
                        if (successorScheduleNode.GetStartTime() == null || successorScheduleNode
                                .GetEndTime().IsGreaterThan(iAsScheduleNode.GetStartTime()))
                        {
                            if (successorScheduleNode.GetType() == typeof(CustomerOrderPart))
                            {
                                throw new MrpRunException(
                                    "Only a root node can be a CustomerOrderPart.");
                            }

                            successorScheduleNode.SetEndTime(iAsScheduleNode.GetStartTime());
                        }

                        _S.Push(successor);
                    }
                }
            }
        }
    }
}