using System.Linq;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.StackSet;

namespace Zpp.Scheduling.impl
{
    public class BackwardScheduler : IBackwardsScheduler
    {
        public void ScheduleBackward(bool clearOldTimes)
        {
            IStackSet<INode> S = new StackSet<INode>();

            IDirectedGraph<INode> orderOperationGraph = new OrderOperationGraph();

            // S = {0} (alle einplanbaren "Operation"=Demand/Provider Elemente)
            S.PushAll(orderOperationGraph.GetRootNodes());

            if (clearOldTimes)
            {
                // d_0 = 0
                foreach (var uniqueNode in orderOperationGraph.GetAllUniqueNodes())
                {
                    uniqueNode.GetEntity().ClearStartTime();
                    uniqueNode.GetEntity().ClearEndTime();
                }
            }


            // while S nor empty do
            while (S.Any())
            {
                INode i = S.PopAny();
                IScheduleNode iAsScheduleNode = i.GetEntity();

                INodes successorNodes = orderOperationGraph.GetSuccessorNodes(i);
                if (successorNodes != null && successorNodes.Any())
                {
                    foreach (var successor in successorNodes)
                    {
                        IScheduleNode successorScheduleNode = successor.GetEntity();
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

                        S.Push(successor);
                    }
                }
            }
        }
    }
}