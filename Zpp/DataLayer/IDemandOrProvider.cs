using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph;

namespace Zpp.DataLayer
{
    public interface IDemandOrProvider: IScheduleNode
    {
        Quantity GetQuantity();
        
        /**
         * For a demand this is usually the startTime, for a provider it's usually the endTime
         */
        DueTime GetDueTime();
    }
}