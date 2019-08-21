using System.Collections;
using System.Collections.Generic;

namespace Zpp
{
    public class CollectionWrapperWithList<T>: ICollectionWrapper<T>
    {
        protected readonly List<T> _list = new List<T>();

        /**
         * Init collectionWrapper with a copy of given list
         */
        protected CollectionWrapperWithList(List<T> list)
        {
            foreach (var item in list)
            {
                _list.Add(item);
            }
        }

        protected CollectionWrapperWithList()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public List<T> GetAll()
        {
            return _list;
        }

        public void Add(T item)
        {
            _list.Add(item);
        }

        public void AddAll(IEnumerable<T> items)
        {
            _list.AddRange(items);
        }
    }
}