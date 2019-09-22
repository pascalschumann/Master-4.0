using System;
using System.Linq;
using Master40.DB;
using Master40.DB.Data.Helper;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zpp.Mrp;

namespace Zpp.Test.Integration_Tests
{
    public class TestTransactionData : AbstractTest
    {
        public TestTransactionData() : base(initDefaultTestConfig: true)
        {
        }

        // TODO: find out, why this is failing on linux/travis and enable this test again
        [Fact(Skip = "disabled, because it doesn't work on linux/travis.'")]
        public void TestEveryCreatedEntityIsPersisted()
        {
            IMrpRun mrpRun = new MrpRun(ProductionDomainContext);
            mrpRun.Start(false);

            ValidateNumberOfEntities(ProductionDomainContext.ProductionOrders);
            ValidateNumberOfEntities(ProductionDomainContext.ProductionOrderBoms);
            
            ValidateNumberOfEntities(ProductionDomainContext.PurchaseOrderParts);
            ValidateNumberOfEntities(ProductionDomainContext.PurchaseOrders);
            ValidateNumberOfEntities(ProductionDomainContext.StockExchanges);
            
            // TODO: enable these for the other transactionData, once there are no missing entities
            // ValidateNumberOfEntities(ProductionDomainContext.ProviderToDemand);
            // ValidateNumberOfEntities(ProductionDomainContext.ProductionOrderOperations);
        }

        private void ValidateNumberOfEntities<TEntity>(DbSet<TEntity> dbSet)
            where TEntity : BaseEntity
        {
            Type typeOfEntity = dbSet.First().GetType();
            int expectedNumberOfEntities = IdGenerator.CountIdsOf(typeOfEntity);
            int actualNumberOfEntities = dbSet.Count();
            Assert.True(expectedNumberOfEntities.Equals(actualNumberOfEntities),
                $"ExpectedNumberOfEntities {expectedNumberOfEntities} doesn't equal " +
                $"actualNumberOfEntities {actualNumberOfEntities} for type {typeOfEntity.Name}.");
        }
    }
}