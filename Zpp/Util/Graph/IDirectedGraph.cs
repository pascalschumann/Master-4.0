using System;
using System.Collections.Generic;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph
{
    /**
     * NOTE: TNode is just a representation of a node, it can occur multiple time
     * and is not a unique runtime object, but equal should return true.
     */
    // TODO: rename From --> Tail, To --> Head
    public interface IDirectedGraph<TNode>
    {
        /**
         * one fromNode has many toNodes
         * @return: toNodes
         */
        INodes GetSuccessorNodes(TNode tailNode);
        
        /// <summary>
        /// traverse graph from leaf upwards to root
        /// </summary>
        /// <param name="predecessorNodes">empty collection, will contain result (the traversed nodes)</param>
        /// <param name="newNodes">collection initialized with your wanted node that is the leaf</param>
        /// <param name="firstRun">must always be true if called from outside</param>
        void GetPredecessorNodesRecursively(INodes predecessorNodes, INodes newNodes, bool firstRun = true);

        INodes GetPredecessorNodes(INode headNode);

        void AddEdges(IEnumerable<IEdge> edges);
        
        void AddEdges(TNode fromNode, INodes nodes);
        
        void AddEdge(IEdge edge);

        int CountEdges();

        IStackSet<INode> GetAllHeadNodes();
        
        IStackSet<INode> GetAllTailNodes();

        /**
         * No duplicates should be contained
         */
        IStackSet<INode> GetAllUniqueNodes();

        IStackSet<IEdge> GetAllEdgesFromTailNode(TNode tailNode);
        
        IStackSet<IEdge> GetAllEdgesTowardsHeadNode(TNode headNode);
        
        INodes TraverseDepthFirst(Action<TNode, INodes, INodes> action, CustomerOrderPart startNode);
     
        /**
         * This removed the node, the edges towards it will point to its childs afterwards
         */
        void RemoveNode(TNode node);

        void RemoveAllEdgesFromTailNode(TNode tailNode);

        void RemoveAllEdgesTowardsHeadNode(TNode headNode);

        INodes GetLeafNodes();
        
        INodes GetRootNodes();

        void ReplaceNodeByDirectedGraph(TNode node, IDirectedGraph<INode> graphToInsert);

        List<IEdge> GetAllEdges();

        IStackSet<IEdge> GetEdges();

        void Clear();

        bool Exists(IEdge edge);

        void RemoveTopDown(INode node);
    }
}