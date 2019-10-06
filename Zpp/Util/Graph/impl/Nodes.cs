using System.Collections.Generic;
using Zpp.DataLayer.WrappersForCollections;

namespace Zpp.Util.Graph.impl
{
    public class Nodes : CollectionWrapperWithStackSet<INode>, INodes
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

        public IEnumerable<T> As<T>()
        {
            List<T> newList = new List<T>();
            foreach (var item in StackSet)
            {
                newList.Add((T)item);
            }

            return newList;
        }
    }
}