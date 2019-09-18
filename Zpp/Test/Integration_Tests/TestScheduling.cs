using Master40.DB.DataModel;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.Scheduling;
using Zpp.OrderGraph;
using Zpp.Test.Configuration;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.Test.Integration_Tests
{
    public class TestScheduling : AbstractTest
    {
        public TestScheduling() : base(false)
        {
        }

        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            MrpRun.Start(ProductionDomainContext);
        }


        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestBackwardScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            foreach (var productionOrderOperation in dbTransactionData
                .ProductionOrderOperationGetAll())
            {
                Assert.True(productionOrderOperation.GetValue().EndBackward != null,
                    $"EndBackward of operation ({productionOrderOperation} is not scheduled.)");
                Assert.True(productionOrderOperation.GetValue().StartBackward != null,
                    $"StartBackward of operation ({productionOrderOperation} is not scheduled.)");
            }
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestForwardScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            foreach (var productionOrderOperation in dbTransactionData
                .ProductionOrderOperationGetAll())
            {
                T_ProductionOrderOperation tProductionOrderOperation =
                    productionOrderOperation.GetValue();
                if (tProductionOrderOperation.StartBackward < 0)
                {
                    Assert.True(
                        tProductionOrderOperation.StartForward != null &&
                        tProductionOrderOperation.EndForward != null,
                        $"Operation ({tProductionOrderOperation}) is not scheduled forward.");
                    Assert.True(
                        tProductionOrderOperation.StartForward >= 0 &&
                        tProductionOrderOperation.EndForward >= 0,
                        "Forward schedule times of operation ({productionOrderOperation}) are negative.");
                }
            }

            List<DueTime> dueTimes = new List<DueTime>();
            foreach (var demand in dbTransactionData.DemandsGetAll())
            {
                dueTimes.Add(demand.GetDueTime(dbTransactionData));
                Assert.True(demand.GetDueTime(dbTransactionData).GetValue() >= 0,
                    $"DueTime of demand ({demand}) is negative.");
            }

            foreach (var provider in dbTransactionData.ProvidersGetAll())
            {
                dueTimes.Add(provider.GetDueTime(dbTransactionData));
                Assert.True(provider.GetDueTime(dbTransactionData).GetValue() >= 0,
                    $"DueTime of provider ({provider}) is negative.");
            }
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOT_ORDER_QUANTITY)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestJobShopScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);
            foreach (var productionOrderOperation in dbTransactionData
                .ProductionOrderOperationGetAll())
            {
                T_ProductionOrderOperation tProductionOrderOperation =
                    productionOrderOperation.GetValue();
                Assert.True(tProductionOrderOperation.Start != tProductionOrderOperation.End,
                    $"{productionOrderOperation} was not scheduled.");
                Assert.True(tProductionOrderOperation.ResourceId != null,
                    $"{productionOrderOperation} was not scheduled.");
                Assert.True(
                    tProductionOrderOperation.Start >= productionOrderOperation
                        .GetDueTimeOfItsMaterial(dbTransactionData).GetValue(),
                    "A productionOrderOperation cannot start before its material is available.");
            }
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOT_ORDER_QUANTITY)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_1_LOTSIZE_1)]
        public void TestParentsDueTimeIsGreaterThanOrEqualToChildsDueTime(
            string testConfigurationFileName)
        {
            // init
            InitThisTest(testConfigurationFileName);
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll())
            {
                Demand parentDemand =
                    dbTransactionData.DemandsGetById(demandToProvider.GetDemandId());
                if (parentDemand.GetType() == typeof(CustomerOrderPart))
                {
                    continue;
                }

                Provider childProvider =
                    dbTransactionData.ProvidersGetById(demandToProvider.GetProviderId());


                DueTime parentDueTime = parentDemand.GetDueTime(dbTransactionData);
                DueTime childDueTime = childProvider.GetDueTime(dbTransactionData);

                Assert.True(parentDueTime.IsGreaterThanOrEqualTo(childDueTime),
                    "ParentDemand's dueTime cannot be smaller than childProvider's dueTime.");
            }

            foreach (var providerToDemand in dbTransactionData.ProviderToDemandGetAll())
            {
                Provider parentProvider =
                    dbTransactionData.ProvidersGetById(providerToDemand.GetProviderId());
                Demand childDemand =
                    dbTransactionData.DemandsGetById(providerToDemand.GetDemandId());

                DueTime parentDueTime = parentProvider.GetDueTime(dbTransactionData);
                DueTime childDueTime = childDemand.GetDueTime(dbTransactionData);

                Assert.True(parentDueTime.IsGreaterThanOrEqualTo(childDueTime),
                    "ParentProvider's dueTime cannot be smaller than childDemand's dueTime.");
            }
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOT_ORDER_QUANTITY)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_1_LOTSIZE_1)]
        public void TestBackwardSchedulingTransitionTimeIsCorrect(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);

            ProductionOrderToOperationGraph productionOrderToOperationGraph =
                new ProductionOrderToOperationGraph(dbMasterDataCache, dbTransactionData);

            IStackSet<INode> innerLeafs = productionOrderToOperationGraph.GetAllInnerLeafs();
            IStackSet<INode> traversedNodes = new StackSet<INode>();
            foreach (var leaf in innerLeafs)
            {
                INodes newPredecessorNodes =
                    productionOrderToOperationGraph.GetPredecessorOperations(leaf);
                ProductionOrderOperation lastOperation = (ProductionOrderOperation) leaf;
                ValidatePredecessorOperationsTransitionTimeIsCorrect(newPredecessorNodes,
                    lastOperation, dbTransactionData, productionOrderToOperationGraph,
                    traversedNodes);
                traversedNodes.Push(leaf.GetEntity());
            }

            int expectedTraversedOperationCount =
                new Stack<ProductionOrderOperation>(dbTransactionData.ProductionOrderOperationGetAll()).Count();
            int actualTraversedOperationCount = traversedNodes.Count();
            
            Assert.True(actualTraversedOperationCount.Equals(expectedTraversedOperationCount),
                $"expectedTraversedOperationCount {expectedTraversedOperationCount} " +
                $"doesn't equal actualTraversedOperationCount {actualTraversedOperationCount}'");
        }

        private void ValidatePredecessorOperationsTransitionTimeIsCorrect(
            INodes predecessorOperations, ProductionOrderOperation lastOperation,
            IDbTransactionData dbTransactionData,
            ProductionOrderToOperationGraph productionOrderToOperationGraph,
            IStackSet<INode> traversedOperations)
        {
            foreach (var currentPredecessor in predecessorOperations)
            {
                if (currentPredecessor.GetEntity().GetType() == typeof(ProductionOrder))
                {
                    INodes newPredecessorNodes =
                        productionOrderToOperationGraph
                            .GetPredecessorOperations(currentPredecessor);
                    ValidatePredecessorOperationsTransitionTimeIsCorrect(newPredecessorNodes,
                        lastOperation, dbTransactionData, productionOrderToOperationGraph,
                        traversedOperations);
                }
                else if (currentPredecessor.GetEntity().GetType() ==
                         typeof(ProductionOrderOperation))
                {
                    ProductionOrderOperation currentOperation =
                        (ProductionOrderOperation) currentPredecessor;
                    traversedOperations.Push(currentPredecessor.GetEntity());

                    // transition time MUST be before the start of Operation
                    int expectedStartBackward =
                        lastOperation.GetValue().EndBackward.GetValueOrDefault() +
                        OperationBackwardsSchedule.GetTransitionTimeFactor() *
                        lastOperation.GetValue().Duration;
                    int actualStartBackward = currentOperation.GetValue().StartBackward
                        .GetValueOrDefault();
                    Assert.True(expectedStartBackward.Equals(actualStartBackward),
                        $"The transition time between the operations is not correct: " +
                        $"expectedStartBackward: {expectedStartBackward}, actualStartBackward {actualStartBackward}");

                    INodes newPredecessorNodes =
                        productionOrderToOperationGraph
                            .GetPredecessorOperations(currentPredecessor);
                    ValidatePredecessorOperationsTransitionTimeIsCorrect(newPredecessorNodes,
                        currentOperation, dbTransactionData, productionOrderToOperationGraph,
                        traversedOperations);
                }
                else
                {
                    throw new MrpRunException(
                        "ProductionOrderToOperationGraph should only contain productionOrders/operations.");
                }
            }
        }
    }
}