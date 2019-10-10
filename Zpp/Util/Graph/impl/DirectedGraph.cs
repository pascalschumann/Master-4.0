using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.GraphicalRepresentation;
using Zpp.GraphicalRepresentation.impl;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph.impl
{
    public class DirectedGraph : IDirectedGraph<INode>
    {
        protected Dictionary<INode, List<IEdge>> _adjacencyList =
            new Dictionary<INode, List<IEdge>>();
        
        private readonly IGraphviz _graphviz = new Graphviz();

        public DirectedGraph()
        {

        }

        public INodes GetSuccessorNodes(INode tailNode)
        {
            if (!_adjacencyList.ContainsKey(tailNode) || _adjacencyList[tailNode].Any() == false)
            {
                return null;
            }

            return new Nodes(_adjacencyList[tailNode].Select(x => x.GetHeadNode()).ToList());
        }

        public void GetPredecessorNodesRecursively(INodes predecessorNodes, INodes newNodes,
            bool firstRun = true)
        {
            INodes newNodes2 = new Nodes();
            foreach (var headNode in newNodes)
            {
                if (GetAllEdgesTowardsHeadNode(headNode) == null)
                {
                    continue;
                }

                foreach (var edge in GetAllEdgesTowardsHeadNode(headNode))
                {
                    newNodes2.Add(edge.GetTailNode());
                }
            }

            if (firstRun == false)
            {
                predecessorNodes.AddAll(newNodes);
            }

            if (newNodes2.Any() == false)
            {
                return;
            }

            GetPredecessorNodesRecursively(predecessorNodes, newNodes2, false);
        }

        public INodes GetPredecessorNodes(INode headNode)
        {
            INodes predecessorNodes = new Nodes();

            if (GetAllEdgesTowardsHeadNode(headNode) == null)
            {
                return null;
            }

            foreach (var edge in GetAllEdgesTowardsHeadNode(headNode))
            {
                INode tailNode = edge.GetTailNode();
                if (tailNode != null && tailNode.GetEntity() != null)
                {
                    predecessorNodes.Add(tailNode);
                }
            }

            if (predecessorNodes.Any() == false)
            {
                return null;
            }

            return predecessorNodes;
        }

        public void AddEdges(INode fromNode, List<IEdge> edges)
        {
            if (!_adjacencyList.ContainsKey(fromNode))
            {
                _adjacencyList.Add(fromNode, edges);
                return;
            }

            _adjacencyList[fromNode].AddRange(edges);
        }

        public void AddEdges(INode fromNode, INodes nodes)
        {
            if (!_adjacencyList.ContainsKey(fromNode))
            {
                _adjacencyList.Add(fromNode, new List<IEdge>());
            }

            foreach (var node in nodes)
            {
                _adjacencyList[fromNode].Add(new Edge(fromNode, node));
            }
        }

        public void AddEdge(INode fromNode, IEdge edge)
        {
            if (!_adjacencyList.ContainsKey(fromNode))
            {
                _adjacencyList.Add(fromNode, new List<IEdge>());
            }

            _adjacencyList[fromNode].Add(edge);
        }

        public int CountEdges()
        {
            return GetAllHeadNodes().Count();
        }

        public List<IEdge> GetAllEdgesFromTailNode(INode tailNode)
        {
            if (_adjacencyList.ContainsKey(tailNode) == false)
            {
                return null;
            }

            return _adjacencyList[tailNode];
        }

        public List<IEdge> GetAllEdgesTowardsHeadNode(INode headNode)
        {
            List<IEdge> edgesTowardsHeadNode = new List<IEdge>();
            foreach (var tailNode in GetAllTailNodes())
            {
                foreach (var edge in _adjacencyList[tailNode])
                {
                    if (edge.GetHeadNode().Equals(headNode))
                    {
                        edgesTowardsHeadNode.Add(edge);
                    }
                }
            }

            if (edgesTowardsHeadNode.Any() == false)
            {
                return null;
            }

            return edgesTowardsHeadNode;
        }

        public override string ToString()
        {
            string mystring = "";
            foreach (var edge in GetAllEdges())
            {
                // foreach (var edge in GetAllEdgesFromTailNode(fromNode))
                // {
                // <Type>, <Menge>, <ItemName> and on edges: <Menge>
                Quantity quantity = null;
                if (edge.GetLinkDemandAndProvider() != null)
                {
                    quantity = edge.GetLinkDemandAndProvider().GetQuantity();
                }

                string tailsGraphvizString =
                    _graphviz.GetGraphizString(edge.GetTailNode().GetEntity());
                string headsGraphvizString =
                    _graphviz.GetGraphizString(edge.GetHeadNode().GetEntity());
                mystring += $"\"{tailsGraphvizString}\" -> " +
                            $"\"{headsGraphvizString}\"";
                // if (quantity.IsNull() == false)
                if (quantity != null && quantity.IsNull() == false)
                {
                    mystring += $" [ label=\" {quantity}\" ]";
                }

                mystring += ";" + Environment.NewLine;
                // }
            }

            return mystring;
        }

        public INodes GetAllHeadNodes()
        {
            List<INode> toNodes = new List<INode>();

            foreach (var edges in _adjacencyList.Values.ToList())
            {
                foreach (var edge in edges)
                {
                    toNodes.Add(edge.GetHeadNode());
                }
            }

            return new Nodes(toNodes);
        }

        /// 
        ///
        /// <summary>
        ///     A depth-first-search (DFS) traversal of given tree
        /// </summary>
        /// <returns>
        ///    The List of the traversed nodes in exact order
        /// </returns>
        public INodes TraverseDepthFirst(Action<INode, INodes, INodes> action,
            CustomerOrderPart startNode)
        {
            var stack = new Stack<INode>();

            Dictionary<INode, bool> discovered = new Dictionary<INode, bool>();
            INodes traversed = new Nodes();

            stack.Push(startNode);
            INode parentNode;

            while (stack.Any())
            {
                INode poppedNode = stack.Pop();

                // init dict if node not yet exists
                if (!discovered.ContainsKey(poppedNode))
                {
                    discovered[poppedNode] = false;
                }

                // if node is not discovered
                if (!discovered[poppedNode])
                {
                    traversed.Add(poppedNode);
                    discovered[poppedNode] = true;
                    INodes childNodes = GetSuccessorNodes(poppedNode);
                    action(poppedNode, childNodes, traversed);

                    if (childNodes != null)
                    {
                        foreach (INode node in childNodes)
                        {
                            stack.Push(node);
                        }
                    }
                }
            }

            return traversed;
        }

        public INodes GetAllTailNodes()
        {
            return new Nodes(_adjacencyList.Keys.ToList());
        }

        public INodes GetAllUniqueNodes()
        {
            INodes fromNodes = GetAllTailNodes();
            INodes toNodes = GetAllHeadNodes();
            IStackSet<INode> uniqueNodes = new StackSet<INode>();
            uniqueNodes.PushAll(fromNodes);
            uniqueNodes.PushAll(toNodes);

            return new Nodes(uniqueNodes);
        }

        public void RemoveNode(INode node)
        {
            List<IEdge> edgesTowardsNode = GetAllEdgesTowardsHeadNode(node);
            List<IEdge> edgesFromNode = GetAllEdgesFromTailNode(node);
            RemoveAllEdgesFromTailNode(node);
            RemoveAllEdgesTowardsHeadNode(node);

            // node is NOT start node AND NOT leaf node
            if (edgesTowardsNode != null && edgesFromNode != null)
            {
                foreach (var edgeTowardsNode in edgesTowardsNode)

                {
                    foreach (var edgeFromNode in edgesFromNode)
                    {
                        AddEdge(edgeTowardsNode.GetTailNode(),
                            new Edge(edgeTowardsNode.GetTailNode(), edgeFromNode.GetHeadNode()));
                    }
                }
            }

            _adjacencyList.Remove(node);
        }

        public void RemoveAllEdgesFromTailNode(INode tailNode)
        {
            _adjacencyList.Remove(tailNode);
        }

        public void RemoveAllEdgesTowardsHeadNode(INode headNode)
        {
            foreach (var tailNode in GetAllTailNodes())
            {
                List<IEdge> edgesToDelete = new List<IEdge>();
                foreach (var edge in _adjacencyList[tailNode])
                {
                    if (edge.GetHeadNode().Equals(headNode))
                    {
                        edgesToDelete.Add(edge);
                    }
                }

                foreach (var edgeToDelete in edgesToDelete)
                {
                    _adjacencyList[tailNode].Remove(edgeToDelete);
                }
            }
        }

        public INodes GetLeafNodes()
        {
            List<INode> leafs = new List<INode>();
            foreach (var uniqueNode in GetAllUniqueNodes())
            {
                INodes successors = GetSuccessorNodes(uniqueNode);
                if (successors == null)
                {
                    leafs.Add(uniqueNode);
                }
            }

            if (leafs.Any() == false)
            {
                return null;
            }

            return new Nodes(leafs);
        }

        public INodes GetRootNodes()
        {
            List<INode> roots = new List<INode>();
            foreach (var uniqueNode in GetAllUniqueNodes())
            {
                INodes predecessor = GetPredecessorNodes(uniqueNode);
                if (predecessor == null)
                {
                    roots.Add(uniqueNode);
                }
            }

            if (roots.Any() == false)
            {
                return null;
            }

            return new Nodes(roots);
        }

        public void ReplaceNodeByDirectedGraph(INode node, IDirectedGraph<INode> graphToInsert)
        {
            INodes predecessors = GetPredecessorNodes(node);
            INodes successors = GetSuccessorNodes(node);
            RemoveNode(node);
            // predecessors --> roots
            if (predecessors != null)
            {
                foreach (var predecessor in predecessors)
                {
                    foreach (var rootNode in graphToInsert.GetRootNodes())
                    {
                        AddEdge(predecessor, new Edge(predecessor, rootNode));
                    }
                }
            }

            // leafs --> successors 
            if (successors != null)
            {
                foreach (var leaf in graphToInsert.GetLeafNodes())
                {
                    foreach (var successor in successors)
                    {
                        AddEdge(leaf, new Edge(leaf, successor));
                    }
                }
            }

            // add all edges from graphToInsert
            foreach (var loopNode in graphToInsert.GetAdjacencyList().Keys)
            {
                AddEdges(loopNode, graphToInsert.GetAdjacencyList()[loopNode]);
            }
        }

        public static IDirectedGraph<INode> MergeDirectedGraphs(
            List<IDirectedGraph<INode>> directedGraphs)
        {
            IDirectedGraph<INode> mergedDirectedGraph = new DirectedGraph();
            foreach (var directedGraph in directedGraphs)
            {
                foreach (var edge in directedGraph.GetAllEdges())
                {
                    mergedDirectedGraph.AddEdge(edge.GetTailNode(), edge);
                }
            }

            return mergedDirectedGraph;
        }

        public List<IEdge> GetAllEdges()
        {
            List<IEdge> allEdges = new List<IEdge>();
            foreach (var edgeList in _adjacencyList.Values)
            {
                allEdges.AddRange(edgeList);
            }

            return allEdges;
        }

        public Dictionary<INode, List<IEdge>> GetAdjacencyList()
        {
            return _adjacencyList;
        }

        public void Clear()
        {
            _adjacencyList = new Dictionary<INode, List<IEdge>>();
        }
    }
}