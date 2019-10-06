using Zpp.DataLayer.ProviderDomain.WrappersForCollections;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph
{
    public interface IProductionOrderToOperationGraph<TNode>: IDirectedGraph<TNode>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="predeccessors">empty StackSet, that will contain the result</param>
        void GetPredecessorOperations(IStackSet<TNode> predecessorOperations, TNode node);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="leafOperations">empty StackSet, that will contain the result</param>
        void GetLeafOperations(IStackSet<TNode> leafOperations);
        ProductionOrderOperations GetAllOperations();

        ProductionOrders GetAllProductionOrders();
    }
}