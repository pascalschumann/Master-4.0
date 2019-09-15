using System;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Mrp.MachineManagement;

namespace Zpp.OrderGraph
{
    public interface ITwoDimensionalGraph<TNode>
    {
        StackSet<INode> GetLeafs();

        INodes GetPredecessors(TNode node);
    }
}