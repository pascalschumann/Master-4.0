using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;
using Zpp.OrderGraph;
using Zpp.WrappersForPrimitives;

namespace Zpp.Mrp.Scheduling
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
            IDirectedGraph<INode> demandToProviderGraph = new DemandToProviderDirectedGraph();

            // S = {0} (alle einplanbaren Operation)
            S.PushAll(demandToProviderGraph.GetLeafNodes());

            // d_0 = 0
            foreach (var node in S)
            {
                IDemandOrProvider demandOrProvider = (IDemandOrProvider) node.GetEntity();
                if (demandOrProvider.GetStartTime().IsNegative())
                {
                    // implicitly the due/endTime will also be set accordingly
                    demandOrProvider.SetStartTime(DueTime.Null());
                }
                else
                {
                }
            }


            // while S nor empty do
            while (S.Any())
            {
                // Entnehme Operation i aus S (beliebig)
                INode i = S.PopAny();
                IDemandOrProvider iAsDemandOrProvider = (IDemandOrProvider) i.GetEntity();


                // t_i = max{ d_j | j aus V(i) }
                INodes predecessors = demandToProviderGraph.GetPredecessorNodes(i);
                // if i != 0 then --> node has predecessor
                if (predecessors != null && predecessors.Any())
                {
                    DueTime d_jMax = null;
                    foreach (var predecessor in predecessors)
                    {
                        IDemandOrProvider predecessorDemandOrProvider =
                            (IDemandOrProvider) predecessor.GetEntity();
                        if (d_jMax == null || predecessorDemandOrProvider.GetEndTime()
                                .IsGreaterThan(d_jMax))
                        {
                            // don't take getDueTime() since in case of a demand,
                            // this will be the startTime, which is to early
                            d_jMax = predecessorDemandOrProvider.GetEndTime();
                        }
                    }

                    iAsDemandOrProvider.SetStartTime(d_jMax);
                    // Done: t_i = max{ d_j | j aus V(i) }

                    // d_i = t_i + p_i
                    // --> is implicitly done by SetStartTime
                }
                // end if
                
                // for all j aus N(i) do
                INodes successors = demandToProviderGraph.GetSuccessorNodes(i);
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
            }
        }
    }
}