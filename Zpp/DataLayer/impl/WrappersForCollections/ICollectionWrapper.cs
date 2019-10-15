using System.Collections.Generic;
using Zpp.Util.StackSet;

namespace Zpp.DataLayer.impl.WrappersForCollections
{
    public interface ICollectionWrapper<T>: IEnumerable<T>
    {
        List<T> GetAll();

        void Add(T item);
        
        void AddAll(IEnumerable<T> items);

        T GetAny();

        IStackSet<T> ToStackSet();

        void Clear();
    }
}