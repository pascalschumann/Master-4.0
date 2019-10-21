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

        /**
         * returns a copy of inner collection as stackSet
         */
        IStackSet<T> ToStackSet();

        void Clear();

        void Remove(T t);
    }
}