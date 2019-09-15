using Master40.DB.DataModel;
using Xunit;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.WrappersForPrimitives;

namespace Zpp.Test.Integration_Tests
{
    public class TestProductionOrderBom : AbstractTest
    {

        public TestProductionOrderBom() : base(initDefaultTestConfig: true)
        {
            MrpRun.Start(ProductionDomainContext);
        }

        [Fact]
        public void TestDueTimeEqualsStartBackwardsOfItsOperation()
        {
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            foreach (var productionOrderBom in dbTransactionData.ProductionOrderBomGetAll())
            {
                DueTime actualDueTime = productionOrderBom.GetDueTime(dbTransactionData);
                int expectedDueTime = ((ProductionOrderBom) productionOrderBom)
                    .GetProductionOrderOperation(dbTransactionData).GetValue().StartBackward
                    .GetValueOrDefault();
                Assert.True(expectedDueTime.Equals(actualDueTime.GetValue()),
                    $"The dueTime of ProductionOrderBom {actualDueTime} MUST be the StartBackwards time " +
                    $"of its operation {expectedDueTime}.");
            }
        }
    }
}