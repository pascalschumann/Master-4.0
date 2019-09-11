using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.Mrp.Scheduling
{
    public class OperationBackwardsSchedule
    {
        private readonly DueTime _timeBetweenOperations;
        private const int TRANSITION_TIME_FACTOR = 3;
        
        private readonly Duration _duration;
        private readonly DueTime _startBackwards;
        private readonly DueTime _startOfOperation;
        private readonly DueTime _endBackwards;
        private readonly HierarchyNumber _hierarchyNumber;

        public OperationBackwardsSchedule(DueTime endBackwards, Duration duration,  HierarchyNumber hierarchyNumber)
        {
            if (endBackwards == null || duration == null || hierarchyNumber == null)
            {
                throw  new MrpRunException("Every parameter must NOT be null.");
            }

            _endBackwards = endBackwards;
            _duration = duration;
            _hierarchyNumber = hierarchyNumber;

            // add slack time aka timeBetweenOperations
            _timeBetweenOperations =
                new DueTime(_duration.GetValue() * TRANSITION_TIME_FACTOR);
            _startBackwards = endBackwards.Minus(duration.GetValue());
            _startOfOperation = endBackwards.Minus(duration.GetValue()).Minus(_timeBetweenOperations);
        }

        public DueTime GetStartBackwards()
        {
            return _startBackwards;
        }
        
        public DueTime GetStartOfOperation()
        {
            return _startOfOperation;
        }
        
        public DueTime GetEndBackwards()
        {
            return _endBackwards;
        }

        public HierarchyNumber GetHierarchyNumber()
        {
            return _hierarchyNumber;
        }
    }
}