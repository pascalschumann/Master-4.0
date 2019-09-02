using Microsoft.EntityFrameworkCore.Internal;
using Xunit;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.Simulation;
using Zpp.Simulation.Types;

namespace Zpp.Test.Simulation
{
    public class TestSimulation : AbstractTest
    {
        private readonly IDbMasterDataCache _dbMasterDataCache;
        private readonly IDbTransactionData _dbTransactionData;
        public TestSimulation() : base(initDefaultTestConfig: true, useLocalDb: true)
        {
            MrpRun.Start(ProductionDomainContext);
            _dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            _dbTransactionData = new DbTransactionData(ProductionDomainContext, _dbMasterDataCache);

        }

        [Fact]
        public void TestSimulationWithResults()
        {
            var Simulator = new Simulator();
            var simulationInterval = new SimulationInterval(0, 1440);
            Simulator.ProcessCurrentInterval(simulationInterval, _dbMasterDataCache, _dbTransactionData);


        }

        [Fact]
        public void ProvideStockExchanges()
        {
            var simulationInterval = new SimulationInterval(0, 1440);
            var stockExchanges = _dbTransactionData.GetAggregator().GetProviderForCurrent(simulationInterval);
            // .GetAll StockExchangeProvidersGetAll().GetAll();
            foreach (var stockExchange in stockExchanges)
            {
                stockExchange.SetProvided(stockExchange.GetDueTime());
            }
            _dbTransactionData.PersistDbCache();
        }
    }
}