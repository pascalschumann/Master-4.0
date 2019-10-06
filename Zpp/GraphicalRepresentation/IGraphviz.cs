using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.Util.Graph;

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