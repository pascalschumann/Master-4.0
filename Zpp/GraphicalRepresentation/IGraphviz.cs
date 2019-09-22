using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.OrderGraph;

namespace Zpp.GraphicalRepresentation
{
    public interface IGraphviz
    {
        string GetGraphizString(CustomerOrderPart customerOrderPart);

        string GetGraphizString(ProductionOrderBom productionOrderBom);

        string GetGraphizString(StockExchangeDemand stockExchangeDemand);

        string GetGraphizString(ProductionOrderOperation productionOrderOperation);

        string GetGraphizString(ProductionOrder productionOrder);

        string GetGraphizString(PurchaseOrderPart purchaseOrderPart);

        string GetGraphizString(StockExchangeProvider stockExchangeProvider);

        string GetGraphizString(INode node);
    }
}