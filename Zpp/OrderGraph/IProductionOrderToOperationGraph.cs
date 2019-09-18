using System;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Mrp.MachineManagement;

namespace Zpp.OrderGraph
{
    public interface IProductionOrderToOperationGraph<TNode>
    {
        StackSet<TNode> GetAllInnerLeafs();

        void Remove(ProductionOrderOperation operation);
        
        void Remove(TNode node);

        void Remove(ProductionOrder productionOrder);

        INodes GetPredecessors(TNode node);
        
        INodes GetPredecessorOperations(TNode node);

        IDirectedGraph<TNode> GetInnerGraph(ProductionOrder productionOrder);

        ProductionOrderOperations GetAllOperations();

        ProductionOrders GetAllProductionOrders();
    }
}