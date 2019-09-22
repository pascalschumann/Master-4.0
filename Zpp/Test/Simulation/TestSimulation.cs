using System.Linq;
using Master40.DB.Enums;
using Master40.SimulationCore.DistributionProvider;
using Master40.SimulationCore.Environment.Options;
using Xunit;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.Simulation;
using Zpp.Simulation.Types;
using Zpp.Test.Configuration.Scenarios;
using Zpp.WrappersForPrimitives;

namespace Zpp.Test.Simulation
{
    public class TestSimulation : AbstractTest
    {
        private readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
        private readonly IDbTransactionData _dbTransactionData;
        private readonly OrderGenerator _orderGenerator;
        public TestSimulation() : base(initDefaultTestConfig: true)
        {
            IMrpRun mrpRun = new MrpRun(ProductionDomainContext);
            mrpRun.Start();
            
            _dbTransactionData = new DbTransactionData(ProductionDomainContext);
            _orderGenerator = TestScenario.GetOrderGenerator(ProductionDomainContext
                                                            , new MinDeliveryTime(960)
                                                            , new MaxDeliveryTime(1440)
                                                            , new OrderArrivalRate(0.025));


        }

        [Fact]
        public void TestSimulationWithResults()
        {
            var Simulator = new Simulator(_dbTransactionData);
            var simulationInterval = new SimulationInterval(0, 1440);
            Simulator.ProcessCurrentInterval(simulationInterval, _orderGenerator);
            _dbTransactionData.PersistDbCache();
            

            // Test for success
            var processedItems = _dbTransactionData.ProductionOrderOperationGetAll()
                .Where(x => x.GetValue().End <= 1800 && x.GetValue().ProducingState != ProducingState.Finished);
            Assert.True(!processedItems.Any());
        }

        [Fact(Skip = "Only for single Execution.")]
        public void ProvideStockExchanges()
        {
            var from = new DueTime(0);
            var to = new DueTime(1440);
            var stockExchanges = _dbTransactionData.GetAggregator().GetProvidersForInterval(from, to);
            // .GetAll StockExchangeProvidersGetAll();
            foreach (var stockExchange in stockExchanges)
            {
                stockExchange.SetProvided(stockExchange.GetDueTime());
            }
            _dbTransactionData.PersistDbCache();
        }
    }
}