using System.Collections;
using System.Collections.Generic;

namespace Zpp
{
    public interface INodes : IEnumerable<INode> // TODO: this should be done for all collectionWrappers
    {
        List<T> GetAllAs<T>();

        List<INode> GetAll();

        void Add(INode node);
    }
}