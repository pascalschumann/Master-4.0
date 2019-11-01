using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph;

namespace Zpp.Util.StackSet
{
    public class StackSet2<T> : IStackSet<T> where T : IId
    {
        private List<T> _list = new List<T>();
        private int _count = 0;
        // index to find Element in list via the element itself
        private Dictionary<T, int> _indexElement = new Dictionary<T, int>();
        // index to find Element in list via the it's id
        private Dictionary<Id, int> _indexId = new Dictionary<Id, int>();

        public StackSet2()
        {
        }

        public StackSet2(IEnumerable<T> list)
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
            if (_indexElement.ContainsKey(element) == false)
            {
                _list.Add(element);
                _indexElement.Add(element, _count);
                _indexId.Add(element.GetId(), _count);
                _count++;
            }
        }

        public void Remove(T element)
        {
            if (element == null)
            {
                return;
            }

            _list.RemoveAt(_indexElement[element]);
            _count--;
            reIndexList();
        }

        private void reIndexList()
        {
            _indexElement = new Dictionary<T, int>();
            _indexId = new Dictionary<Id, int>();
            for (int i = 0; i < _count; i++)
            {
                _indexElement.Add(_list[i], i);
                _indexId.Add(_list[i].GetId(), i);
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
            return new StackSet2<T2>(_list.Select(x => (T2) x));
        }

        public void Clear()
        {
            _list = new List<T>();
            _count = 0;
            _indexElement = new Dictionary<T, int>();
            _indexId = new Dictionary<Id, int>();
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

        public T GetById(Id id)
        {
            if (_indexId.ContainsKey(id))
            {
                return _list[_indexId[id]];    
            }
            else
            {
                return default(T);
            }
        }

        public bool Contains(T t)
        {
            return _indexElement.ContainsKey(t);
        }

        public bool Contains(Id id)
        {
            return _indexId.ContainsKey(id);
        }
    }
}