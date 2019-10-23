using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zpp.Util.StackSet
{
    public class StackSet<T> : IStackSet<T>
    {
        private List<T> _list = new List<T>();
        private int _count = 0;
        private Dictionary<T, int> _indices = new Dictionary<T, int>();

        public StackSet()
        {
        }

        public StackSet(IEnumerable<T> list)
        {
            PushAll(list);
        }

        public void Push(T element)
        {
            if (element == null)
            {
                return;
            }

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
            if (element == null)
            {
                return;
            }

            _list.RemoveAt(_indices[element]);
            _count--;
            reIndexList();
        }

        private void reIndexList()
        {
            _indices = new Dictionary<T, int>();
            for (int i = 0; i < _count; i++)
            {
                _indices.Add(_list[i], i);
            }
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

        public List<T2> GetAllAs<T2>() where T2 : T
        {
            List<T2> list = new List<T2>();
            foreach (var item in GetAll())
            {
                list.Add((T2) item);
            }

            return list;
        }

        public IStackSet<T2> As<T2>() where T2 : T
        {
            return new StackSet<T2>(_list.Select(x => (T2) x));
        }

        public void Clear()
        {
            _list = new List<T>();
            _count = 0;
            _indices = new Dictionary<T, int>();
        }

        public override string ToString()
        {
            string result = "";
            
            foreach (var item in _list)
            {
                result += item.ToString() + Environment.NewLine;
            }

            return result;
        }
    }
}