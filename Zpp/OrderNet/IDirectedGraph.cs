using System;
using System.Collections.Generic;
using Zpp.DemandDomain;
using Zpp.GraphicalRepresentation;
using Zpp.Utils;

namespace Zpp
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
        List<TNode> GetSuccessorNodes(TNode tailNode);
        
        List<TNode> GetPredecessorNodes(TNode headNode);

        void AddEdges(TNode fromNode, List<IEdge> edges);
        
        void AddEdge(TNode fromNode, IEdge edge);

        int CountEdges();

        List<INode> GetAllHeadNodes();
        
        List<INode> GetAllTailNodes();

        /**
         * No duplicates should be contained
         */
        List<INode> GetAllUniqueNode();

        List<IEdge> GetAllEdgesFromTailNode(INode tailNode);
        
        List<IEdge> GetAllEdgesTowardsHeadNode(INode headNode);
        
        List<INode> TraverseDepthFirst(Action<INode, List<INode>, List<INode>> action, CustomerOrderPart startNode);

        GanttChart GetAsGanttChart(IDbTransactionData dbTransactionData);

        /**
         * This removed the node, the edges towards it will point to its childs afterwards
         */
        void RemoveNode(INode node);

        void RemoveAllEdgesFromTailNode(INode tailNode);

        void RemoveAllEdgesTowardsHeadNode(INode headNode);

        List<INode> GetLeafNodes();

        List<INode> GetStartNodes();

        void ReplaceNodeByDirectedGraph(INode node);

        List<IEdge> GetAllEdges();

        Dictionary<INode, List<IEdge>> GetAdjacencyList();
    }
}