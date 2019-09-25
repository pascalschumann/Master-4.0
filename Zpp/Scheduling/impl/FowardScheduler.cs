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
        public static DueTime FindMinDueTime(IDemands demands, IProviders providers)
        {
            DueTime minDueTime = null;

            // find min dueTime
            foreach (var provider in providers)
            {
                DueTime currentDueTime = provider.GetDueTime();
                if (minDueTime == null)
                {
                    minDueTime = currentDueTime;
                }

                if (minDueTime.GetValue() > currentDueTime.GetValue())
                {
                    minDueTime = currentDueTime;
                }
            }

            return minDueTime;
        }

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
        */
            IStackSet<IDemandOrProvider> S = new StackSet<IDemandOrProvider>();
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IDirectedGraph<INode> demandToProviderGraph = new DirectedGraph();

            // S = {0} (alle einplanbaren Operation)
            S.PushAll(demandToProviderGraph.GetLeafNodes().As<IDemandOrProvider>());

            // d_0 = 0
            foreach (var node in S)
            {
                if (node.GetDueTime().IsNegative())
                {
                    node.SetDueTime(DueTime.Null());
                }
            }


            // while S nor empty do
            while (S.Any())
            {
                // Entnehme Operation i aus S (beliebig)
                IDemandOrProvider demandOrProvider = S.PopAny();

                /*if i != 0 then
                    t_i = max{ d_j | j aus V(i) }
                    d_i = t_i + p_i
                end if
                for all j aus N(i) do
                    z_j--
                    if z_j == 0 then
                        S = S vereinigt {j}
                    end if
                end for*/
            }
        }
    }
}