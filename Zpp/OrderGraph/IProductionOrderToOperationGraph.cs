using System;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Mrp.MachineManagement;

namespace Zpp.OrderGraph
{
    public interface IProductionOrderToOperationGraph<TNode>
    {
        StackSet<TNode> GetLeafs();

        void Remove(ProductionOrderOperation operation);
        
        void Remove(TNode node);

        void Remove(ProductionOrder productionOrder);

        INodes GetPredecessors(TNode node);

        IDirectedGraph<TNode> GetInnerGraph(ProductionOrder productionOrder);
    }
}