using System.Collections.Generic;
using Zpp.WrappersForCollections;

namespace Zpp.OrderGraph
{
    public interface INodes : ICollectionWrapper<INode> // TODO: this should be done for all collectionWrappers
    {
        IEnumerable<T> As<T>();
    }
}