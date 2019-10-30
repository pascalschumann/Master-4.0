using Master40.DB.Data.Helper;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Interfaces;

namespace Zpp.Util.Graph.impl
{
    public class Edge : IEdge
    {
        private readonly ILinkDemandAndProvider _demandToProvider;
        private readonly INode _tailNode;
        private readonly INode _headNode;
        private readonly Id _id = IdGeneratorHolder.GetIdGenerator().GetNewId();

        public Edge(ILinkDemandAndProvider demandToProvider, INode tailNode, INode toNode)
        {
            _demandToProvider = demandToProvider;
            _tailNode = tailNode;
            _headNode = toNode;
        }

        public Edge(INode tailNode, INode toNode)
        {
            _tailNode = tailNode;
            _headNode = toNode;
        }

        public INode GetTailNode()
        {
            return _tailNode;
        }

        public INode GetHeadNode()
        {
            return _headNode;
        }

        public ILinkDemandAndProvider GetLinkDemandAndProvider()
        {
            return _demandToProvider;
        }

        public override string ToString()
        {
            return $"{_tailNode} --> {_headNode}";
        }

        public override bool Equals(object obj)
        {
            Edge other = (Edge) obj;
            bool headAndTailAreEqual =
                _headNode.Equals(other._headNode) && _tailNode.Equals(other._tailNode);
            if (_demandToProvider == null)
            {
                return headAndTailAreEqual && _demandToProvider == other._demandToProvider;
            }
            else
            {
                return headAndTailAreEqual && _demandToProvider.Equals(other._demandToProvider);
            }
        }

        public override int GetHashCode()
        {
            return _headNode.GetHashCode() + _tailNode.GetHashCode();
        }

        public Id GetId()
        {
            return _id;
        }
    }
}