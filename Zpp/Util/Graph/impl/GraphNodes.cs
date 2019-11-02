using System.Collections.Generic;
using Zpp.DataLayer.impl.WrappersForCollections;

namespace Zpp.Util.Graph.impl
{
    public class GraphNodes: CollectionWrapperWithStackSet<IGraphNode>
    {
        internal GraphNodes(IEnumerable<IGraphNode> list) : base(list)
        {
        }

        internal GraphNodes()
        {
        }
    }
}