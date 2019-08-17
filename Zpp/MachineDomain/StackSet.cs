using System;
using System.Collections;
using System.Collections.Generic;

namespace Zpp.MachineDomain
{
    public class StackSet<T>:IStackSet<T>
    {
        private List<T> _list = new List<T>();
        private int _count = 0;
        private Dictionary<T, int> _indices = new Dictionary<T, int>();
        
        public void Push(T element)
        {
            // a set contains the element only once, else skip adding
            if (_indices.ContainsKey(element) == false)
            {
                _list.Add(element);
                _indices.Add(element, _count);
                _count++;
                
            }
        }

        public void Remove(T element)
        {
            _list.RemoveAt(_indices[element]);
            _count--;
        }

        public bool Any()
        {
            return _count > 0;
        }

        public T PopAny()
        {
            T element = _list[0];
            _list.RemoveAt(0);
            _count--;
            return element;
        }

        public T GetAny()
        {
            return _list[0];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public void PushAll(IEnumerable<T> elements)
        {
            foreach (var element in elements)
            {
                Push(element);
            }
        }

        public int Count()
        {
            return _count;
        }

        public List<T> GetAll()
        {
            // create a copy of list
            List<T> all = new List<T>();
            all.AddRange(_list);
            return all;
        }
    }
}