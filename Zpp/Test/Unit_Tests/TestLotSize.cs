using Master40.DB.Data.WrappersForPrimitives;
using Xunit;
using Zpp.Configuration;
using Zpp.DataLayer;

namespace Zpp.Test.Unit_Tests
{
    public class TestLotSize : AbstractTest
    {
        [Fact]
        public void TestALotSize()
        {
            IDbMasterDataCache dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();

            LotSize.Impl.LotSize lotSize = new LotSize.Impl.LotSize(new Quantity(6),
                dbMasterDataCache.M_ArticleGetAll()[0].GetId());
            foreach (var quantity in lotSize.GetLotSizes())
            {
                Assert.True(quantity.GetValue() == TestConfiguration.LotSize, $"Quantity ({quantity}) is not correct.");
            }
        }
    }
}