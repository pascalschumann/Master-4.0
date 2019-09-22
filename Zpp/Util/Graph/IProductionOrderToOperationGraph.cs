using System;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Mrp.MachineManagement;

namespace Zpp.OrderGraph
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