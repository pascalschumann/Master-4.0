using System.Data;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using MathNet.Numerics.Random;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.OrderGraph;
using Zpp.Utils;

namespace Zpp.GraphicalRepresentation
{
    public class Graphviz : IGraphviz
    {
        private readonly ICacheManager _cacheManager =
            ZppConfiguration.CacheManager;


        private string ToGraphizString(Demand demand)
        {
            return $"{demand.GetQuantity()};\\n{demand.GetArticle().Name};" +
                   $" Start/End/Due: {demand.GetStartTime()}/{demand.GetEndTime()}/{demand.GetDueTime()};";
        }

        private string ToGraphizString(Provider provider)
        {
            return $"{provider.GetQuantity()};\\n{provider.GetArticle().Name};" +
                   $" Start/End/Due: {provider.GetStartTime()}/{provider.GetEndTime()}/{provider.GetDueTime()};";
        }

        public string GetGraphizString(CustomerOrderPart customerOrderPart)
        {
            // Demand(CustomerOrder);20;Truck
            string graphizString = $"D(COP);{ToGraphizString(customerOrderPart)}";
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
                graphizString = $"D(PrOB);{ToGraphizString(productionOrderBom)};" +
                                $"bs({tProductionOrderOperation.StartBackward});" +
                                $"be({tProductionOrderOperation.EndBackward});" +
                                $"\\n{tProductionOrderOperation}";
            }
            else
            {
                graphizString = $"D(PrOB);{ToGraphizString(productionOrderBom)}";
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
                $"D(SE:{exchangeType[0]});{ToGraphizString(stockExchangeDemand)}";
            return graphizString;
        }

        public string GetGraphizString(ProductionOrderOperation productionOrderOperation)
        {
            return $"{productionOrderOperation.GetValue().Name}";
        }

        public string GetGraphizString(ProductionOrder productionOrder)
        {
            // Demand(CustomerOrder);20;Truck
            string graphizString = $"P(PrO);{ToGraphizString(productionOrder)}";
            return graphizString;
        }

        public string GetGraphizString(PurchaseOrderPart purchaseOrderPart)
        {
            // Demand(CustomerOrder);20;Truck
            string graphizString = $"P(PuOP);{ToGraphizString(purchaseOrderPart)}";
            return graphizString;
        }

        public string GetGraphizString(StockExchangeProvider stockExchangeProvider)
        {
            // Demand(CustomerOrder);20;Truck
            string exchangeType = Constants.EnumToString(
                ((T_StockExchange) stockExchangeProvider.ToIProvider()).ExchangeType,
                typeof(ExchangeType));
            string graphizString =
                $"P(SE:{exchangeType[0]});{ToGraphizString(stockExchangeProvider)}";
            return graphizString;
        }

        public string GetGraphizString(INode node)
        {
            INode entity = node.GetEntity();
            switch (entity)
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