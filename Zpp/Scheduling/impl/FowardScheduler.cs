using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.Util.StackSet;

namespace Zpp.Scheduling.impl
{
    public class ForwardScheduler : IForwardScheduler
    {
        public void ScheduleForward()
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
            IStackSet<INode> S = new StackSet<INode>();
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDirectedGraph<INode> demandToProviderGraph = new DemandToProviderGraph();

            // S = {0} (alle einplanbaren "Operation"=Demand/Provider Elemente)
            S.PushAll(demandToProviderGraph.GetLeafNodes());

            // d_0 = 0
            IStackSet<INode> newS = new StackSet<INode>();
            foreach (var node in S)
            {
                IDemandOrProvider demandOrProvider = (IDemandOrProvider) node.GetEntity();
                if (demandOrProvider.GetStartTime().IsNegative())
                {
                    // implicitly the due/endTime will also be set accordingly
                    demandOrProvider.SetStartTime(DueTime.Null());
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
                INode i = S.PopAny();
                IDemandOrProvider iAsDemandOrProvider = (IDemandOrProvider) i.GetEntity();


                // t_i = max{ d_j | j aus V(i) }
                // predecessors in d2pGraph must start later (exact the other way around)
                INodes predecessors = demandToProviderGraph.GetPredecessorNodes(i);
                // if i != 0 then --> node has predecessor
                if (predecessors != null && predecessors.Any())
                {
                    foreach (var predecessor in predecessors)
                    {
                        IDemandOrProvider predecessorDemandOrProvider =
                            (IDemandOrProvider) predecessor.GetEntity();
                        // if predecessor starts before endTime of current d/p --> change that
                        if (predecessorDemandOrProvider.GetStartTime()
                                .IsSmallerThan(iAsDemandOrProvider.GetEndTime()))
                        {
                            // COPs are not allowed to change
                            if (predecessorDemandOrProvider.GetType() != typeof(CustomerOrderPart))
                            {
                                // don't take getDueTime() since in case of a demand,
                                // this will be the startTime, which is to early
                                predecessorDemandOrProvider.SetStartTime(iAsDemandOrProvider.GetEndTime());    
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