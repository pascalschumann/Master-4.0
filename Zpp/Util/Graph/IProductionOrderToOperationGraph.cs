using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph
{
    public interface IProductionOrderToOperationGraph<TNode>: IDirectedGraph<TNode>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="predeccessors">empty StackSet, that will contain the result</param>
        void GetPredecessorOperations(IStackSet<ProductionOrderOperation> predecessorOperations, TNode node);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="leafOperations">empty StackSet, that will contain the result</param>
        void GetLeafOperations(IStackSet<ProductionOrderOperation> leafOperations);
        ProductionOrderOperations GetAllOperations();

        ProductionOrders GetAllProductionOrders();
    }
}