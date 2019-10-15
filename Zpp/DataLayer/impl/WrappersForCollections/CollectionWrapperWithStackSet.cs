using System.Collections;
using System.Collections.Generic;
using Zpp.Util.StackSet;

namespace Zpp.DataLayer.impl.WrappersForCollections
{
    public class CollectionWrapperWithStackSet<T>: ICollectionWrapper<T>
    {
        protected readonly IStackSet<T> StackSet = new StackSet<T>();

        /**
         * Init collectionWrapper with a copy of given list
         */
        protected CollectionWrapperWithStackSet(IEnumerable<T> list)
        {
            foreach (var item in list)
            {
                StackSet.Push(item);
            }
        }
        
        protected CollectionWrapperWithStackSet(T item)
        {
            StackSet.Push(item);
        }

        protected CollectionWrapperWithStackSet()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return StackSet.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return StackSet.GetEnumerator();
        }

        public List<T> GetAll()
        {
            return StackSet.GetAll();
        }

        public void Add(T item)
        {
            StackSet.Push(item);
        }

        public void AddAll(IEnumerable<T> items)
        {
            StackSet.PushAll(items);
        }

        public T GetAny()
        {
            return StackSet.GetAny();
        }

        public IStackSet<T> ToStackSet()
        {
            return new StackSet<T>(StackSet);
        }

        public void Clear()
        {
            StackSet.Clear();
        }
    }
}