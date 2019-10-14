using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.StackSet;

namespace Zpp.Scheduling.impl
{
    public class BackwardScheduler : IBackwardsScheduler
    {
        private readonly Stack<INode> _S;
        private readonly IDirectedGraph<INode> _orderOperationGraph;
        private readonly bool _clearOldTimes;

        public BackwardScheduler(Stack<INode> S, IDirectedGraph<INode> orderOperationGraph,
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
                INode i = _S.Pop();
                IScheduleNode iAsScheduleNode = i.GetEntity();

                INodes successorNodes = _orderOperationGraph.GetSuccessorNodes(i);
                if (successorNodes != null && successorNodes.Any())
                {
                    foreach (var successor in successorNodes)
                    {
                        IScheduleNode successorScheduleNode = successor.GetEntity();

                        // Konservativ vorwärtsterminieren ist korrekt,
                        // aber rückwärts muss wenn immer möglich terminiert werden
                        // (prüfe parents und ermittle minStart und setze das)
                        INodes predecessorNodes =
                            _orderOperationGraph.GetPredecessorNodes(successor);
                        DueTime minStartTime = iAsScheduleNode.GetStartTime();
                        foreach (var predecessorNode in predecessorNodes)
                        {
                            DueTime predecessorsStartTime =
                                predecessorNode.GetEntity().GetStartTime();
                            if (predecessorsStartTime != null &&
                                predecessorsStartTime.IsSmallerThan(minStartTime))
                            {
                                minStartTime = predecessorsStartTime;
                            }
                        }

                        if (successorScheduleNode.GetType() == typeof(CustomerOrderPart))
                        {
                            throw new MrpRunException(
                                "Only a root node can be a CustomerOrderPart.");
                        }

                        successorScheduleNode.SetEndTime(minStartTime);

                        _S.Push(successor);
                    }
                }
            }
        }
    }
}