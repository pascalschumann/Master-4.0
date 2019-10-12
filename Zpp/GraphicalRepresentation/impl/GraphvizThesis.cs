using Master40.DB.DataModel;
using Master40.DB.Enums;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.Util;
using Zpp.Util.Graph;

namespace Zpp.GraphicalRepresentation.impl
{
    public class GraphvizThesis : IGraphviz
    {

        private string ToGraphizString(Demand demand)
        {
            return $"\\n{demand.GetId()}: {demand.GetArticle().Name};\\nMenge: {demand.GetQuantity()};";
        }

        private string ToGraphizString(Provider provider)
        {
            return $"\\n{provider.GetId()}: {provider.GetArticle().Name};\\nMenge: {provider.GetQuantity()};";
        }

        public string GetGraphizString(CustomerOrderPart customerOrderPart)
        {
            // Demand(CustomerOrder);20;Truck
            string graphizString = $"D: Kundenauftragsposition;{ToGraphizString(customerOrderPart)}";
            return graphizString;
        }

        public string GetGraphizString(ProductionOrderBom productionOrderBom)
        {
            // Demand(CustomerOrder);20;Truck

            string graphizString;
            ProductionOrderOperation productionOrderOperation =
                productionOrderBom.GetProductionOrderOperation();
            if (productionOrderOperation != null)
            {
                T_ProductionOrderOperation tProductionOrderOperation =
                    productionOrderOperation.GetValue();
                graphizString = $"D: Produktionsauftragsposition;{ToGraphizString(productionOrderBom)}";
            }
            else
            {
                throw new MrpRunException("Every productionOrderBom must have exact one operation.");
            }

            return graphizString;
        }

        public string GetGraphizString(StockExchangeDemand stockExchangeDemand)
        {
            // Demand(CustomerOrder);20;Truck
            string exchangeType = Constants.EnumToString(
                ((T_StockExchange) stockExchangeDemand.ToIDemand()).ExchangeType,
                typeof(ExchangeType));
            string graphizString =
                $"D: Lagereingang;{ToGraphizString(stockExchangeDemand)}";
            return graphizString;
        }

        public string GetGraphizString(ProductionOrderOperation productionOrderOperation)
        {
            return $"{productionOrderOperation.GetValue().Name}";
        }

        public string GetGraphizString(ProductionOrder productionOrder)
        {
            // Demand(CustomerOrder);20;Truck
            string graphizString = $"P: Produktionsauftrag;{ToGraphizString(productionOrder)}";
            return graphizString;
        }

        public string GetGraphizString(PurchaseOrderPart purchaseOrderPart)
        {
            // Demand(CustomerOrder);20;Truck
            string graphizString = $"P: Bestellposition;{ToGraphizString(purchaseOrderPart)}";
            return graphizString;
        }

        public string GetGraphizString(StockExchangeProvider stockExchangeProvider)
        {
            // Demand(CustomerOrder);20;Truck
            string exchangeType = Constants.EnumToString(
                ((T_StockExchange) stockExchangeProvider.ToIProvider()).ExchangeType,
                typeof(ExchangeType));
            string graphizString =
                $"P: Lagerausgang;{ToGraphizString(stockExchangeProvider)}";
            return graphizString;
        }

        public string GetGraphizString(IScheduleNode node)
        {
            switch (node)
            {
                case StockExchangeProvider t1:
                    return GetGraphizString(t1);
                case PurchaseOrderPart t2:
                    return GetGraphizString(t2);
                case ProductionOrder t3:
                    return GetGraphizString(t3);
                case ProductionOrderOperation t4:
                    return GetGraphizString(t4);
                case StockExchangeDemand t5:
                    return GetGraphizString(t5);
                case ProductionOrderBom t6:
                    return GetGraphizString(t6);
                case CustomerOrderPart t7:
                    return GetGraphizString(t7);
                default: throw new MrpRunException("Call getEntity() before calling this method.");
            }
        }
    }
}