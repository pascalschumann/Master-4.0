using Master40.DB.DataModel;
using Master40.DB.Interfaces;

namespace Zpp.OrderGraph
{
    public interface IEdge
    {
        INode GetTailNode();

        INode GetHeadNode();

        ILinkDemandAndProvider GetLinkDemandAndProvider();
    }
}