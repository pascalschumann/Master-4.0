using System.Collections.Generic;
using Zpp.WrappersForCollections;

namespace Zpp.OrderGraph
{
    public class Nodes : CollectionWrapperWithList<INode>, INodes
    {
        public Nodes(IEnumerable<INode> list) : base(list)
        {
        }

        public Nodes(INode item) : base(item)
        {
        }

        public Nodes()
        {
        }
    }
}