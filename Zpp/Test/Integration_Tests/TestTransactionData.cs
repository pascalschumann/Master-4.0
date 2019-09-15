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

        [Fact]
        public void TestEveryCreatedEntityIsPersisted()
        {
            MrpRun.Start(ProductionDomainContext, false);

            ValidateNumberOfEntities(ProductionDomainContext.ProductionOrders);
            ValidateNumberOfEntities(ProductionDomainContext.ProductionOrderBoms);
            
            ValidateNumberOfEntities(ProductionDomainContext.PurchaseOrderParts);
            ValidateNumberOfEntities(ProductionDomainContext.PurchaseOrders);
            ValidateNumberOfEntities(ProductionDomainContext.StockExchanges);
            
            ValidateNumberOfEntities(ProductionDomainContext.ProviderToDemand);
            ValidateNumberOfEntities(ProductionDomainContext.ProductionOrderOperations);
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