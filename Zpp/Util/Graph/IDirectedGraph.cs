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
        INodes GetSuccessorNodes(TNode node);
        
        INodes GetPredecessorNodes(INode node);

        void AddEdges(IEnumerable<IEdge> edges);
        
        void AddEdges(TNode fromNode, INodes nodes);
        
        void AddEdge(IEdge edge);

        int CountEdges();

        /**
         * No duplicates should be contained
         */
        IStackSet<INode> GetAllUniqueNodes();

        INodes TraverseDepthFirst(Action<TNode, INodes, INodes> action, CustomerOrderPart startNode);
     
        /**
         * This removes the node, the edges towards it will point to its childs afterwards
         */
        void RemoveNode(TNode node);

        INodes GetLeafNodes();
        
        INodes GetRootNodes();

        void ReplaceNodeByDirectedGraph(TNode node, IDirectedGraph<INode> graphToInsert);

        List<IEdge> GetAllEdges();

        IStackSet<IEdge> GetEdges();

        INodes GetNodes();

        void AddNodes(INodes nodes);

        void AddNode(INode node);

        void Clear();

        void RemoveTopDown(INode node);

        bool IsEmpty();
        
        // replaces ToString() since debugger is unusable
        string AsString();
    }
}