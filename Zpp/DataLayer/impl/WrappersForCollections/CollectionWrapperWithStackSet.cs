using System.Collections;
using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph;
using Zpp.Util.StackSet;

namespace Zpp.DataLayer.impl.WrappersForCollections
{
    public class CollectionWrapperWithStackSet<T>: ICollectionWrapper<T> where T: IId
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
            return StackSet;
        }

        public void Clear()
        {
            StackSet.Clear();
        }

        public void Remove(T t)
        {
            StackSet.Remove(t);
        }

        public T GetById(Id id)
        {
            return StackSet.GetById(id);
        }

        public string AsString()
        {
            return StackSet.AsString();
        }
    }
}