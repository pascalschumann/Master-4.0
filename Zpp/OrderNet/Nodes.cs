using System.Collections;
using System.Collections.Generic;

namespace Zpp
{
    public class Nodes : INodes
    {
        private readonly List<INode> _nodes;

        public Nodes(List<INode> nodes)
        {
            _nodes = nodes;
        }

        public List<T> GetAllAs<T>()
        {
            List<T> typedNodes = new List<T>();
            foreach (var node in _nodes)
            {
                typedNodes.Add((T)node.GetEntity());
            }

            return typedNodes;
        }

        public List<INode> GetAll()
        {
            return _nodes;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        public IEnumerator<INode> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }
    }
}