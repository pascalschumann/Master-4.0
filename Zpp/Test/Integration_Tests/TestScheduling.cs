using Master40.DB.DataModel;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Xunit;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.Mrp;
using Zpp.Scheduling.impl;
using Zpp.Test.Configuration;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.Util.StackSet;
using Zpp.ZppSimulator;

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

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
        }


        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestBackwardScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

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
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestForwardScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);

            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

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
                dueTimes.Add(demand.GetDueTime());
                Assert.True(demand.GetDueTime().GetValue() >= 0,
                    $"DueTime of demand ({demand}) is negative.");
            }

            foreach (var provider in dbTransactionData.ProvidersGetAll())
            {
                dueTimes.Add(provider.GetDueTime());
                Assert.True(provider.GetDueTime().GetValue() >= 0,
                    $"DueTime of provider ({provider}) is negative.");
            }
        }

        [Theory]
        
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestJobShopScheduling(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
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
                        .GetEarliestPossibleStartTime().GetValue(),
                    "A productionOrderOperation cannot start before its material is available.");
            }
        }

        [Theory]
        
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        
        public void TestParentsDueTimeIsGreaterThanOrEqualToChildsDueTime(
            string testConfigurationFileName)
        {
            // init
            InitThisTest(testConfigurationFileName);
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

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


                DueTime parentDueTime = parentDemand.GetDueTime();
                DueTime childDueTime = childProvider.GetDueTime();

                Assert.True(parentDueTime.IsGreaterThanOrEqualTo(childDueTime),
                    "ParentDemand's dueTime cannot be smaller than childProvider's dueTime.");
            }

            foreach (var providerToDemand in dbTransactionData.ProviderToDemandGetAll())
            {
                Provider parentProvider =
                    dbTransactionData.ProvidersGetById(providerToDemand.GetProviderId());
                Demand childDemand =
                    dbTransactionData.DemandsGetById(providerToDemand.GetDemandId());

                DueTime parentDueTime = parentProvider.GetDueTime();
                DueTime childDueTime = childDemand.GetDueTime();

                Assert.True(parentDueTime.IsGreaterThanOrEqualTo(childDueTime),
                    "ParentProvider's dueTime cannot be smaller than childDemand's dueTime.");
            }
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestBackwardSchedulingTransitionTimeForeachOperationIsCorrect(
            string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            foreach (var productionOrderBom in dbTransactionData
                .ProductionOrderBomGetAll().GetAllAs<ProductionOrderBom>())
            {
                int expectedStartBackward =
                    productionOrderBom.GetStartTime().GetValue() +
                    OperationBackwardsSchedule.GetTransitionTimeFactor() *
                    productionOrderBom.GetDurationOfOperation().GetValue();
                int actualStartBackward = productionOrderBom.GetStartTimeOfOperation().GetValue();
                Assert.True(expectedStartBackward.Equals(actualStartBackward),
                    $"The transition time before operationStart is not correct: " +
                    $"expectedStartBackward: {expectedStartBackward}, actualStartBackward {actualStartBackward}");

                int expectedEndBackward =
                    productionOrderBom.GetStartTimeOfOperation().GetValue() + 
                    productionOrderBom.GetDurationOfOperation().GetValue();
                int actualEndBackward = productionOrderBom.GetEndTime().GetValue();
                Assert.True(expectedEndBackward.Equals(actualEndBackward),
                    $"EndBackward is not correct: " +
                    $"expectedEndBackward: {expectedEndBackward}, actualEndBackward {actualEndBackward}");
            }
        }

        [Theory]
        
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        
        public void TestBackwardSchedulingTransitionTimeBetweenOperationsIsCorrect(
            string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            ProductionOrderToOperationGraph productionOrderToOperationGraph =
                new ProductionOrderToOperationGraph();

            IStackSet<INode> innerLeafs =
                productionOrderToOperationGraph.GetLeafNodes().ToStackSet();
            IStackSet<INode> traversedNodes = new StackSet<INode>();
            foreach (var leaf in innerLeafs)
            {
                IStackSet<INode> newPredecessorNodes = new StackSet<INode>();
                productionOrderToOperationGraph.GetPredecessorOperations(newPredecessorNodes, leaf);
                ProductionOrderOperation lastOperation = (ProductionOrderOperation) leaf;
                ValidatePredecessorOperationsTransitionTimeIsCorrect(newPredecessorNodes,
                    lastOperation, productionOrderToOperationGraph,
                    traversedNodes);
                traversedNodes.Push(leaf.GetEntity());
            }

            int expectedTraversedOperationCount =
                new Stack<ProductionOrderOperation>(
                    dbTransactionData.ProductionOrderOperationGetAll()).Count();
            int actualTraversedOperationCount = traversedNodes.Count();

            Assert.True(actualTraversedOperationCount.Equals(expectedTraversedOperationCount),
                $"expectedTraversedOperationCount {expectedTraversedOperationCount} " +
                $"doesn't equal actualTraversedOperationCount {actualTraversedOperationCount}'");
        }

        private void ValidatePredecessorOperationsTransitionTimeIsCorrect(
            IStackSet<INode> predecessorOperations, ProductionOrderOperation lastOperation,
            
            ProductionOrderToOperationGraph productionOrderToOperationGraph,
            IStackSet<INode> traversedOperations)
        {
            if (predecessorOperations == null)
            {
                return;
            }

            foreach (var currentPredecessor in predecessorOperations)
            {
                if (currentPredecessor.GetEntity().GetType() == typeof(ProductionOrderOperation))
                {
                    ProductionOrderOperation currentOperation =
                        (ProductionOrderOperation) currentPredecessor.GetEntity();
                    traversedOperations.Push(currentPredecessor.GetEntity());

                    // transition time MUST be before the start of Operation
                    int expectedStartBackward =
                        lastOperation.GetValue().EndBackward.GetValueOrDefault() +
                        OperationBackwardsSchedule.GetTransitionTimeFactor() *
                        currentOperation.GetValue().Duration;
                    int actualStartBackward = currentOperation.GetValue().StartBackward
                        .GetValueOrDefault();
                    Assert.True(expectedStartBackward.Equals(actualStartBackward),
                        $"The transition time between the operations is not correct: " +
                        $"expectedStartBackward: {expectedStartBackward}, actualStartBackward {actualStartBackward}");

                    IStackSet<INode> newPredecessorNodes = new StackSet<INode>();
                    productionOrderToOperationGraph.GetPredecessorOperations(newPredecessorNodes,
                        currentPredecessor);
                    ValidatePredecessorOperationsTransitionTimeIsCorrect(newPredecessorNodes,
                        currentOperation, productionOrderToOperationGraph,
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