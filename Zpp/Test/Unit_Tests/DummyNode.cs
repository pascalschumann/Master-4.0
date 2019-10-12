using Master40.DB.Data.Helper;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.Test.Unit_Tests
{
    public class DummyNode:INode
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
            throw new System.NotImplementedException();
        }

        public NodeType GetNodeType()
        {
            return NodeType.Operation;
        }

        public override string ToString()
        {
            return _id.ToString();
        }
    }
}