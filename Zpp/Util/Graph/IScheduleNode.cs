using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Enums;
using Zpp.Util.Graph.impl;

namespace Zpp.Util.Graph
{
    public interface IScheduleNode
    {
        DueTime GetEndTime();

        DueTime GetStartTime();

        /**
         * Adapts the startTime and also adapts the endTime accordingly (if exists)
         */
        void SetStartTime(DueTime startTime);

        /**
         * Adapts the endTime and also adapts the startTime accordingly (if exists)
         */
        void SetEndTime(DueTime endTime);

        /**
         * Contains transition time if exits
         */
        Duration GetDuration();

        void SetDone();

        void SetInProgress();

        bool IsDone();

        NodeType GetNodeType();

        Id GetId();

        void ClearStartTime();

        void ClearEndTime();

        State? GetState();

        /**
         * There comes a time, when an entity is finished e.g. ProductionOrder is finished producing
         * --> entity is not allowed to change anymore regarding time/amount
         * OR an initial StockExchangeDemand that simulates the initial stock is not allowed to change in time
         */
        void SetReadOnly();
    }
}