using Master40.DB.Data.Helper;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.Test.Unit_Tests
{
    public class DummyNode:IScheduleNode
    {
        
        private Id _id;

        public DummyNode(Id id)
        {
            _id = id;
        }

        public Id GetId()
        {
            return _id;
        }

        public IScheduleNode GetEntity()
        {
            return this;
        }

        public NodeType GetNodeType()
        {
            return NodeType.Operation;
        }

        public override string ToString()
        {
            return _id.ToString();
        }

        public DueTime GetEndTime()
        {
            throw new System.NotImplementedException();
        }

        public DueTime GetStartTime()
        {
            throw new System.NotImplementedException();
        }

        public void SetStartTime(DueTime startTime)
        {
            throw new System.NotImplementedException();
        }

        public Duration GetDuration()
        {
            throw new System.NotImplementedException();
        }

        public void SetDone()
        {
            throw new System.NotImplementedException();
        }

        public void SetInProgress()
        {
            throw new System.NotImplementedException();
        }

        public bool IsDone()
        {
            throw new System.NotImplementedException();
        }

        public void SetEndTime(DueTime endTime)
        {
            throw new System.NotImplementedException();
        }

        public void ClearStartTime()
        {
            throw new System.NotImplementedException();
        }

        public void ClearEndTime()
        {
            throw new System.NotImplementedException();
        }
    }
}