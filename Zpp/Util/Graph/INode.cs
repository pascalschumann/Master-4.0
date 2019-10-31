using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph.impl;

namespace Zpp.Util.Graph
{
    public interface INode: IId
    {
    
        NodeType GetNodeType();

        IScheduleNode GetEntity();

        void AddSuccessor(INode node);
        
        void AddSuccessors(INodes nodes);
        
        

        void AddPredecessor(INode node);
        
        void AddPredecessors(INodes nodes);
        
        INodes GetPredecessors();
        
        INodes GetSuccessors();

        void RemoveSuccessor(INode node);
        
        void RemovePredecessor(INode node);
        
        void RemoveAllSuccessors();
        
        void RemoveAllPredecessors();
    }
}