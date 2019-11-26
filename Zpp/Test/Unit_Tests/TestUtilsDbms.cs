using Master40.DB.Data.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zpp.Util;

namespace Zpp.Test.Unit_Tests
{
    public class TestUtilsDbms
    {
        [Fact(Skip = "Sql server 'drop database' does not work on non-Windows-systems.")]
        public void TestDropExistingDatabase()
        {
            ProductionDomainContexts productionDomainContexts = Dbms.GetDbContext();
            ProductionDomainContext productionDomainContext =
                productionDomainContexts.ProductionDomainContext;
            
            if (productionDomainContext.Database.CanConnect() == false)
            {
                productionDomainContext.Database.EnsureCreated();
            }

            productionDomainContext.Database.CloseConnection();

            bool wasDropped =
                Dbms.DropDatabase(productionDomainContext.Database.GetDbConnection().Database,
                    productionDomainContext.Database.GetDbConnection().ConnectionString);
            Assert.True(wasDropped, "Db could not be dropped.");
            Assert.False(productionDomainContext.Database.CanConnect(),
                "Can still connect to database.");
        }

        [Fact]
        public void TestDropNonExistingDatabase()
        {
            ProductionDomainContexts productionDomainContexts = Dbms.GetDbContext();
            ProductionDomainContext productionDomainContext =
                productionDomainContexts.ProductionDomainContext;
            
            bool wasDropped = Dbms.DropDatabase("bla",
                productionDomainContext.Database.GetDbConnection().ConnectionString);
            Assert.False(wasDropped, "Db could be dropped, although it doesn't exist.");
        }
    }
}