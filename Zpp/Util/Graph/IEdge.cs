using Master40.DB.Interfaces;

namespace Zpp.Util.Graph
{
    public interface IEdge
    {
        INode GetTailNode();

        INode GetHeadNode();

        ILinkDemandAndProvider GetLinkDemandAndProvider();
    }
}