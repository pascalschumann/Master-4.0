using System.Collections.Generic;
using Zpp.Mrp.MachineManagement;

namespace Zpp.WrappersForCollections
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