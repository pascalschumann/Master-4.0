using Master40.DB.Data.WrappersForPrimitives;
using Master40.SimulationCore.DistributionProvider;
using Master40.SimulationCore.Environment.Options;
using Zpp.DataLayer;
using Zpp.Test.Configuration.Scenarios;

namespace Zpp.ZppSimulator.impl.CustomerOrder.impl
{
    public class CustomerOrderCreator: ICustomerOrderCreator
    {
        private IOrderGenerator _orderGenerator = null;
        
        public void CreateCustomerOrders(SimulationInterval interval)
        {
            CreateCustomerOrders(interval, null);
        }

        public void CreateCustomerOrders(SimulationInterval interval, Quantity customerOrderQuantity)
        {
            IDbMasterDataCache masterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            OrderArrivalRate orderArrivalRate;
            if (customerOrderQuantity == null)
            {
                orderArrivalRate = new OrderArrivalRate(0.025);
            }
            else
            {
                // (Menge der zu erzeugenden auftrage im intervall +1) / (die dauer des intervalls)
                // works only small numbers e.g. 10
                orderArrivalRate =
                    new OrderArrivalRate((double) (customerOrderQuantity.GetValue() * 2) /
                                         interval.Interval);
            }

            if (_orderGenerator == null ||
                _orderGenerator.GetOrderArrivalRate().Equals(orderArrivalRate) == false)
            {
                _orderGenerator = TestScenario.GetOrderGenerator(new MinDeliveryTime(200),
                    new MaxDeliveryTime(1430), orderArrivalRate, masterDataCache.M_ArticleGetAll(),
                    masterDataCache.M_BusinessPartnerGetAll());
            }

            var creationTime = interval.StartAt;
            var endOrderCreation = interval.EndAt;

            // Generate exact given quantity of customerOrders
            while (creationTime < endOrderCreation &&
                   dbTransactionData.T_CustomerOrderGetAll().Count <
                   customerOrderQuantity.GetValue())
            {
                var order = _orderGenerator.GetNewRandomOrder(time: creationTime);
                foreach (var orderPart in order.CustomerOrderParts)
                {
                    orderPart.CustomerOrder = order;
                    orderPart.CustomerOrderId = order.Id;
                    dbTransactionData.CustomerOrderPartAdd(orderPart);
                }

                dbTransactionData.CustomerOrderAdd(order);

                // TODO : Handle this another way (Why Martin ?)
                creationTime += order.CreationTime;
            }
        }
    }
}