using System.Collections.Generic;
using Zpp.DataLayer.impl.WrappersForCollections;

namespace Zpp.Util.Graph
{
    public interface INodes : ICollectionWrapper<INode> // TODO: this should be done for all collectionWrappers
    {
        IEnumerable<T> As<T>();

        Stack<INode> ToStack();
    }
}