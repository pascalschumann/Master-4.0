using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Xunit;
using Zpp.DataLayer;
using Zpp.Test.Configuration;
using Zpp.Test.Integration_Tests;
using Zpp.ZppSimulator.impl;
using Zpp.ZppSimulator.impl.CustomerOrder.impl;

namespace Zpp.Test.Unit_Tests
{
    public class TestOrderGenerator : AbstractTest
    {
        public TestOrderGenerator() : base()
        {
        }

        [Fact]
        public void TestGeneratedQuantity()
        {
            TestConfiguration testConfiguration =
                ZppConfiguration.CacheManager.GetTestConfiguration();
            int cycles = testConfiguration.SimulationMaximumDuration /
                         testConfiguration.SimulationInterval;
            int totalCustomerOrdersToCreate = 500;
            int customerOrdersToCreate = totalCustomerOrdersToCreate / (cycles + 1); // round up


            CustomerOrderCreator og = new CustomerOrderCreator(null);
            og.CreateCustomerOrders(new SimulationInterval(0, 20160),
                new Quantity(totalCustomerOrdersToCreate));
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            var orders = dbTransactionData.CustomerOrderGetAll();
            Assert.InRange(orders.Count(), customerOrdersToCreate, customerOrdersToCreate+10);
        }
    }
}