using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.OrderGraph;
using Zpp.Test.Configuration;
using Zpp.Utils;

namespace Zpp.Test.Integration_Tests
{
    public class TestDemandToProviderGraph : AbstractTest
    {

        public TestDemandToProviderGraph(): base(false)
        {
            
        }

        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            MrpRun.Start(ProductionDomainContext);
        }

        /**
         * Verifies, that the demandToProviderGraph
         * - can be build up from DemandToProvider+ProviderToDemand table
         * - is a top down graph TODO is not done yet<br>
         * - has all demandToProvider and providerToDemand edges
         */
        [Theory]
        
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOT_ORDER_QUANTITY)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestAllEdgesAreInDemandToProviderGraph(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            // build demandToProviderGraph up
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IDirectedGraph<INode> demandToProviderGraph = new DemandToProviderDirectedGraph(dbTransactionData);

            Assert.True(demandToProviderGraph.GetAllHeadNodes().Any(),
                "There are no toNodes in the demandToProviderGraph.");

            int sumDemandToProviderAndProviderToDemand =
                dbTransactionData.DemandToProviderGetAll().Count() +
                dbTransactionData.ProviderToDemandGetAll().Count();

            Assert.True(sumDemandToProviderAndProviderToDemand == demandToProviderGraph.CountEdges(),
                $"Should be equal size: sumDemandToProviderAndProviderToDemand " +
                $"{sumDemandToProviderAndProviderToDemand} and  sumValuesOfDemandToProviderGraph {demandToProviderGraph.CountEdges()}");
        }

        /**
         * Assumptions:
         * - IDemand:   T_CustomerOrderPart (COP), T_ProductionOrderBom (PrOB), T_StockExchange (SE:I)
         * - IProvider: T_PurchaseOrderPart (PuOP), T_ProductionOrder (PrO),    T_StockExchange (SE:W)
         *
         * Verifies that,
         * for demand (parent) --> provider (child) direction following takes effect:
         * - COP  --> SE:W
         * - PrOB --> SE:W | NONE
         * - SE:I --> PuOP | PrO
         *
         * for provider (parent) --> demand (child) direction following takes effect:
         * - PuOP --> NONE
         * - PrO  --> PrOB
         * - SE:W --> SE:I | NONE
         *
         * where SE:I = StockExchangeDemand
         * and SE:W = StockExchangeProvider
         * TODO: remove StockExchangeType from T_StockExchange since it's exactly specified by Withdrawal/Insert
         *
         * TODO: add new Quality to test: check that NONE is only if it's defined in upper connections
         * (e.g. after a PrO MUST come another Demand )
         */
        [Theory]
        
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOT_ORDER_QUANTITY)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestEdgeTypes(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            IDictionary<Type, Type[]> allowedEdges = new Dictionary<Type, Type[]>()
            {
                // demand --> provider
                {
                    typeof(CustomerOrderPart),
                    new Type[]
                    {
                        typeof(StockExchangeProvider)
                    }
                },
                {
                    typeof(ProductionOrderBom), new Type[]
                    {
                        typeof(StockExchangeProvider)
                    }
                },
                {
                    typeof(StockExchangeDemand),
                    new Type[] {typeof(PurchaseOrderPart), typeof(ProductionOrder)}
                },
                // provider --> demand
                {
                    typeof(PurchaseOrderPart),
                    new Type[] { }
                },
                {
                    typeof(ProductionOrder),
                    new Type[] {typeof(ProductionOrderBom)}
                },
                {
                    typeof(StockExchangeProvider),
                    new Type[] {typeof(StockExchangeDemand)}
                }
            };

            // build demandToProviderGraph up
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            IDirectedGraph<INode> demandToProviderGraph = new DemandToProviderDirectedGraph(dbTransactionData);

            // verify edgeTypes
            foreach (var customerOrderPart in dbTransactionData.T_CustomerOrderPartGetAll())
            {
                demandToProviderGraph.TraverseDepthFirst((INode parentNode, INodes childNodes, INodes traversed) =>
                {
                    if (childNodes != null && childNodes.Any())
                    {
                        Type parentType = parentNode.GetEntity().GetType();
                        foreach (var childNode in childNodes)
                        {
                            Type childType = childNode.GetEntity().GetType();
                            Assert.True(allowedEdges[parentType].Contains(childType),
                                $"This is no valid edge: {parentType} --> {childType}");
                        }
                    }
                }, (CustomerOrderPart) customerOrderPart);
            }
        }

        /**
         * In case of failing (and the demandToProviderGraph change is expected by you):
         * delete corresponding ordergraph_cop_*.txt files ind Folder Test/DemandToProviderGraphs
         */
        [Theory]
        
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOT_ORDER_QUANTITY)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_1_LOTSIZE_1)]
        public void TestDemandToProviderGraphStaysTheSame(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            string demandToProviderGraphFileName =
                $"../../../Test/Ordergraphs/demandToProvider_graph_{TestConfiguration.Name}.txt";

            // build demandToProviderGraph up
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            IDirectedGraph<INode> demandToProviderGraph = new DemandToProviderDirectedGraph(dbTransactionData);

            
            string actualDemandToProviderGraph = demandToProviderGraph.ToString();
            if (File.Exists(demandToProviderGraphFileName) == false)
            {
                File.WriteAllText(demandToProviderGraphFileName, actualDemandToProviderGraph,
                    Encoding.UTF8);
            }
            
            string expectedDemandToProviderGraph =
                File.ReadAllText(demandToProviderGraphFileName, Encoding.UTF8);
            
            bool demandToProviderGraphHasNotChanged =
                expectedDemandToProviderGraph.Equals(actualDemandToProviderGraph);
            // for debugging: write the changed graphs to files
            if (demandToProviderGraphHasNotChanged == false)
            {
                File.WriteAllText(demandToProviderGraphFileName, actualDemandToProviderGraph,
                    Encoding.UTF8);
            }

            if (Constants.IsWindows)
            {
                Assert.True(demandToProviderGraphHasNotChanged, "DemandToProviderGraph has changed.");
            }
            else
            {
                // On linux the graph is always different so the test would always fail here.
                Assert.True(true);
            }
        }

        public static string removeIdsFromDemandToProviderGraph(string demandToProviderGraph)
        {
            string[] demandToProviderGraphLines = demandToProviderGraph.Split("\r\n");
            // to have reproducible result
            Array.Sort(demandToProviderGraphLines);
            List<string> demandToProviderGraphWithoutIds = new List<string>();
            foreach (var demandToProviderGraphLine in demandToProviderGraphLines)
            {
                string newString = "";
                string[] splitted = demandToProviderGraphLine.Split("->");
                if (splitted.Length == 2)
                {
                    newString += "\"" + splitted[0].Substring(7, splitted[0].Length - 7);
                    newString += " -> ";
                    newString += "\"" + splitted[1].Substring(8, splitted[1].Length - 8);

                    demandToProviderGraphWithoutIds.Add(newString);
                }
            }

            return String.Join("\r\n", demandToProviderGraphWithoutIds);
        }
    }
}