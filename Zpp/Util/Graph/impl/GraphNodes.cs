using System.Collections.Generic;
using Zpp.DataLayer.impl.WrappersForCollections;

namespace Zpp.Util.Graph.impl
{
    internal class GraphNodes: CollectionWrapperWithStackSet<IGraphNode>
    {
        internal GraphNodes(IEnumerable<IGraphNode> list) : base(list)
        {
        }

        internal GraphNodes()
        {
        }
    }
}