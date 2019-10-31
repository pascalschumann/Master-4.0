using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.Util.Graph;

namespace Zpp.Mrp2.impl.Scheduling.impl
{
    public class ForwardScheduler : IForwardScheduler
    {
        private readonly OrderOperationGraph _orderOperationGraph;

        public ForwardScheduler(OrderOperationGraph orderOperationGraph)
        {
            _orderOperationGraph = orderOperationGraph;
        }

        public void ScheduleForward()
        {
            Stack<INode> S = new Stack<INode>();

            // d_0 = 0
            foreach (var node in _orderOperationGraph.GetLeafNodes())
            {
                IScheduleNode scheduleNode = node.GetEntity();
                if (scheduleNode.GetStartTimeBackward().IsNegative())
                {
                    // implicitly the due/endTime will also be set accordingly
                    scheduleNode.SetStartTimeBackward(DueTime.Null());
                    S.Push(node);
                }
                else // no forward scheduling is needed
                {
                }
            }


            // while S nor empty do
            while (S.Any())
            {
                INode i = S.Pop();
                IScheduleNode iAsScheduleNode = (IScheduleNode) i.GetEntity();

                INodes predecessors = _orderOperationGraph.GetPredecessorNodes(i);
                if (predecessors != null && predecessors.Any())
                {
                    foreach (var predecessor in predecessors)
                    {
                        IScheduleNode predecessorScheduleNode = predecessor.GetEntity();
                        
                        // if predecessor starts before endTime of current d/p --> change that
                        if (predecessorScheduleNode.IsReadOnly() == false && predecessorScheduleNode
                                .GetStartTimeBackward().IsSmallerThan(iAsScheduleNode.GetEndTimeBackward()))
                        {
                            // COPs are not allowed to change
                            if (predecessorScheduleNode.GetType() != typeof(CustomerOrderPart))
                            {
                                // don't take getDueTime() since in case of a demand,
                                // this will be the startTime, which is to early

                                // This must be the maximum endTime of all childs !!!
                                DueTime maxEndTime = iAsScheduleNode.GetEndTimeBackward();
                                foreach (var successor in _orderOperationGraph.GetSuccessorNodes(
                                    predecessor))
                                {
                                    DueTime successorsEndTime = successor.GetEntity().GetEndTimeBackward();
                                    if (successorsEndTime.IsGreaterThan(maxEndTime))
                                    {
                                        maxEndTime = successorsEndTime;
                                    }
                                }

                                predecessorScheduleNode.SetStartTimeBackward(maxEndTime);
                            }
                        }

                        S.Push(predecessor);
                    }
                }
            }
        }

        public void ScheduleForwardAsZaepfel()
        {
            /*
            S: Menge der einplanbaren Arbeitsoperationen
            p_i: Dauer der Arbeitsoperation i
            t_i: Frühestmöglicher Anfangszeitpunkt von Operation i
            d_i: Frühestmöglicher Endzeitpunkt von Operation i
            V(i): Menge der direkten Vorgänger (im Graphen) von Operation i
            N(i): Menge der direkten Nachfolger von Operation i
            z_i: Anzahl der noch nicht eingeplanten direkten Vorgänger von Operation i
            0: DummyOperation, von der zu allen ersten Operationen eine Verbindung vorhanden ist
        */
            Stack<INode> S = new Stack<INode>();
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDirectedGraph<INode> orderOperationGraph = new OrderOperationGraph();

            // S = {0} (alle einplanbaren "Operation"=Demand/Provider Elemente)
            foreach (var leaf in orderOperationGraph.GetLeafNodes())
            {
                S.Push(leaf);
            }

            // d_0 = 0
            Stack<INode> newS = new Stack<INode>();
            foreach (var node in S)
            {
                IScheduleNode scheduleNode = (IScheduleNode) node.GetEntity();
                if (scheduleNode.GetStartTimeBackward().IsNegative())
                {
                    // implicitly the due/endTime will also be set accordingly
                    scheduleNode.SetStartTimeBackward(DueTime.Null());
                    newS.Push(node);
                }
                else // no forward scheduling is needed
                {
                }
            }

            S = newS;


            // while S nor empty do
            while (S.Any())
            {
                // Entnehme Operation i aus S (beliebig)
                INode i = S.Pop();
                IScheduleNode iAsScheduleNode = (IScheduleNode) i.GetEntity();


                // t_i = max{ d_j | j aus V(i) }
                // predecessors in d2pGraph must start later (exact the other way around)
                INodes predecessors = orderOperationGraph.GetPredecessorNodes(i);
                // if i != 0 then --> node has predecessor
                if (predecessors != null && predecessors.Any())
                {
                    foreach (var predecessor in predecessors)
                    {
                        IScheduleNode predecessorScheduleNode =
                            (IScheduleNode) predecessor.GetEntity();
                        // if predecessor starts before endTime of current d/p --> change that
                        if (predecessorScheduleNode.GetStartTimeBackward()
                            .IsSmallerThan(iAsScheduleNode.GetEndTimeBackward()))
                        {
                            // COPs are not allowed to change
                            if (predecessorScheduleNode.GetType() != typeof(CustomerOrderPart))
                            {
                                // don't take getDueTime() since in case of a demand,
                                // this will be the startTime, which is to early
                                predecessorScheduleNode.SetStartTimeBackward(iAsScheduleNode.GetEndTimeBackward());
                            }
                        }

                        S.Push(predecessor);
                    }
                    // Done: t_i = max{ d_j | j aus V(i) }

                    // d_i = t_i + p_i
                    // --> is implicitly done by SetStartTime
                }
                // end if

                // for all j aus N(i) do
                /*INodes successors = demandToProviderGraph.GetSuccessorNodes(i);
                if (successors != null)
                {
                    foreach (var j in successors)
                    {
                        // z_j--
                        // --> not necessary, since z_j is always 0 here (we traverse the d2p graph
                        // bottom upwards and remove the leafs at the end of a loop iteration)
                        // if z_j == 0 then
                        // --> always the case
                        // S = S vereinigt {j}
                        S.Push(j);
                        // end if
                        // --> ignored
                    }
                }

                // end for
                */
                // --> already done for predecessors, we traverse d2pG bottom up, so we only need to process predecessors
            }
        }
    }
}