using Master40.DB.Interfaces;

namespace Zpp.Util.Graph.impl
{
    public class Edge : IEdge
    {
        private readonly ILinkDemandAndProvider _demandToProvider;
        private readonly INode _tailNode;
        private readonly INode _headNode;

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
    }
}