using System;
using Microsoft.EntityFrameworkCore.Internal;
using Xunit;
using Zpp.DemandDomain;
using Zpp.ProviderDomain;
using Zpp.Simulation;
using Zpp.Simulation.Types;

namespace Zpp.Test
{
    public class TestSimulation : AbstractTest
    {
        public TestSimulation() : base(initDefaultTestConfig: true, useLocalDb: true)
        {
            MrpRun.RunMrp(ProductionDomainContext);
        }

        [Fact]
        public void TestSimulationWithResults()
        {
            var Simulator = new Simulator();
            var simulationInval = new SimulationInterval(0, 1440);
            Simulator.ProcessCurrentInterval(simulationInval, ProductionDomainContext);

            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            Providers providers = dbTransactionData.GetAggregator()
                .GetAllProviderOfDemand(dbTransactionData.DemandsGetAll().GetAll()[0],
                    dbTransactionData);
            Assert.True(providers.Any());
            ProductionOrderBoms productionOrderBoms = dbTransactionData.GetAggregator()
                .GetAllProductionOrderBomsBy(dbTransactionData.ProductionOrderOperationGetAll()[0]);
            Assert.True(productionOrderBoms.Any());
        }
    }
}