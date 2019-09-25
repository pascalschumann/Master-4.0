using Master40.DB.Data.WrappersForPrimitives;
using Zpp.OrderGraph;
using Zpp.WrappersForPrimitives;

namespace Zpp.DataLayer
{
    public interface IDemandOrProvider: INode
    {
        DueTime GetDueTime();

        void SetDueTime(DueTime dueTime);
        
        DueTime GetStartTime();

        void SetStartTime(DueTime dueTime);
    }
}