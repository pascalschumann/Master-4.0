using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.WrappersForPrimitives;

namespace Zpp.Mrp.Scheduling
{
    public class ForwardScheduler
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
    }
}