using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Xunit;
using Zpp.DataLayer;
using Zpp.Test.Integration_Tests;
using Zpp.ZppSimulator.impl;
using Zpp.ZppSimulator.impl.CustomerOrder.impl;

namespace Zpp.Test.Unit_Tests
{
    public class TestOrderGenerator : AbstractTest
    {
        public TestOrderGenerator() : base() { }

        [Fact]
        public void TestGeneratedQuantity()
        {
            CustomerOrderCreator og = new CustomerOrderCreator(new Quantity(500));
            og.CreateCustomerOrders( new SimulationInterval(0, 20160));
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            var orders = dbTransactionData.CustomerOrderPartGetAll();
            Assert.InRange(orders.Count(), 450, 550);
        }
    }
}
