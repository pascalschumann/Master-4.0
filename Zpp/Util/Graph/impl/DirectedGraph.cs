using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Interfaces;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.GraphicalRepresentation;
using Zpp.GraphicalRepresentation.impl;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph.impl
{
    /**
     * An impl for a directed graph. It's important to always return null if aggregation is empty
     * (simplify error detecting, since no empty collections should pass through the program).
     */
    public class DirectedGraph : IDirectedGraph<INode>
    {
        protected IStackSet<IEdge> Edges = new StackSet<IEdge>();

        protected readonly IGraphviz Graphviz = new Graphviz();

        public DirectedGraph()
        {
        }

        public INodes GetSuccessorNodes(INode tailNode)
        {
            INodes successors = new Nodes(Edges.Where(x => x.GetTailNode().Equals(tailNode))
                .Select(x => x.GetHeadNode()).ToList());
            if (successors.Any() == false)
            {
                return null;
            }
            return successors;
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

        public void AddEdges(IEnumerable<IEdge> edges)
        {
            Edges.PushAll(edges);
        }

        public void AddEdges(INode fromNode, INodes nodes)
        {
            foreach (var toNode in nodes)
            {
                AddEdge(new Edge(fromNode, toNode));
            }
        }

        public void AddEdge(IEdge edge)
        {
            Edges.Push(edge);
        }

        public int CountEdges()
        {
            return GetAllHeadNodes().Count();
        }

        public List<IEdge> GetAllEdgesFromTailNode(INode tailNode)
        {
            List<IEdge> edges = Edges.Where(x => x.GetTailNode().Equals(tailNode)).ToList();
            if (edges.Any() == false)
            {
                return null;
            }
            return edges;
        }

        public List<IEdge> GetAllEdgesTowardsHeadNode(INode headNode)
        {
            List<IEdge> edges = Edges.Where(x => x.GetHeadNode().Equals(headNode)).ToList();
            if (edges.Any() == false)
            {
                return null;
            }
            return edges;
        }

        public override string ToString()
        {
            string mystring = "";
            foreach (var edge in GetAllEdges())
            {
                string tailsGraphvizString =
                    Graphviz.GetGraphizString(edge.GetTailNode().GetEntity());
                string headsGraphvizString =
                    Graphviz.GetGraphizString(edge.GetHeadNode().GetEntity());
                mystring += $"\"{tailsGraphvizString}\" -> " + $"\"{headsGraphvizString}\"";

                mystring += ";" + Environment.NewLine;
                // }
            }

            return mystring;
        }

        public INodes GetAllHeadNodes()
        {
            INodes headNodes = new Nodes(Edges.Select(x => x.GetHeadNode()));
            if (headNodes.Any() == false)
            {
                return null;
            }
            return headNodes;
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
            INodes tailNodes = new Nodes(Edges.Select(x=>x.GetTailNode()));
            if (tailNodes.Any() == false)
            {
                return null;
            }
            return tailNodes;
        }

        public INodes GetAllUniqueNodes()
        {
            INodes fromNodes = GetAllTailNodes();
            INodes toNodes = GetAllHeadNodes();
            IStackSet<INode> uniqueNodes = new StackSet<INode>();
            uniqueNodes.PushAll(fromNodes);
            uniqueNodes.PushAll(toNodes);
            INodes uniqueNodesAsINodes = new Nodes(uniqueNodes);
            if (uniqueNodesAsINodes.Any() == false)
            {
                return null;
            }
            return uniqueNodesAsINodes;
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
                        AddEdge(new Edge(edgeTowardsNode.GetTailNode(), edgeFromNode.GetHeadNode()));
                    }
                }
            }
        }

        public void RemoveAllEdgesFromTailNode(INode tailNode)
        {
            List<IEdge> edges = Edges.Where(x => x.GetTailNode().Equals(tailNode)).ToList();
            foreach (var edge in edges)
            {
                Edges.Remove(edge);    
            }
            
        }

        public void RemoveAllEdgesTowardsHeadNode(INode headNode)
        {
            List<IEdge> edges = Edges.Where(x => x.GetHeadNode().Equals(headNode)).ToList();
            foreach (var edge in edges)
            {
                Edges.Remove(edge);    
            }
        }

        public INodes GetLeafNodes()
        {
            List<INode> leafs = new List<INode>();
            foreach (var uniqueNode in GetAllUniqueNodes())
            {
                INodes successors = GetSuccessorNodes(uniqueNode);
                if (successors == null || successors.Any() == false)
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
                        AddEdge(new Edge(predecessor, rootNode));
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
                        AddEdge(new Edge(leaf, successor));
                    }
                }
            }

            // add all edges from graphToInsert
            AddEdges(graphToInsert.GetEdges().GetAll());
        }

        public static IDirectedGraph<INode> MergeDirectedGraphs(
            List<IDirectedGraph<INode>> directedGraphs)
        {
            IDirectedGraph<INode> mergedDirectedGraph = new DirectedGraph();
            foreach (var directedGraph in directedGraphs)
            {
                foreach (var edge in directedGraph.GetAllEdges())
                {
                    mergedDirectedGraph.AddEdge(edge);
                }
            }

            return mergedDirectedGraph;
        }

        public List<IEdge> GetAllEdges()
        {
            if (Edges.Any() == false)
            {
                return null;
            }
            return Edges.GetAll();
        }

        public IStackSet<IEdge> GetEdges()
        {
            if (Edges.Any() == false)
            {
                return null;
            }
            return Edges;
        }

        public void Clear()
        {
            Edges.Clear();
        }

        public bool Exists(IEdge edge)
        {
            return Edges.Any(x=>x.Equals(edge));
        }
    }
}