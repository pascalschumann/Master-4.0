using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.SimulationCore.Helper;
using Master40.XUnitTest.DBContext;
using Xunit;
using Zpp.DemandDomain;
using Zpp.ProviderDomain;
using Zpp.Test.WrappersForPrimitives;

namespace Zpp.Test
{
    public class TestOrderGraph : AbstractTest
    {
        private const int ORDER_QUANTITY = 6;
        private const int DEFAULT_LOT_SIZE = 2;

        public TestOrderGraph()
        {
            OrderGenerator.GenerateOrdersSyncron(ProductionDomainContext,
                ContextTest.TestConfiguration(), 1, true, ORDER_QUANTITY);
            LotSize.LotSize.SetDefaultLotSize(new Quantity(DEFAULT_LOT_SIZE));

            MrpRun.RunMrp(ProductionDomainContext);
        }

        /**
         * Verifies, that the orderGraph
         * - can be build up from DemandToProvider+ProviderToDemand table
         * - is a top down graph TODO is not done yet<br>
         * - has all demandToProvider and providerToDemand edges
         */
        [Fact]
        public void TestAllEdgesAreInOrderGraph()
        {
            // build orderGraph up
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);
            
                IGraph<INode> orderGraph = new OrderGraph(dbTransactionData);

                Assert.True(orderGraph.GetAllToNodes().Count > 0,
                    "There are no toNodes in the orderGraph.");

                int sumDemandToProviderAndProviderToDemand =
                    dbTransactionData.DemandToProviderGetAll().Count() +
                    dbTransactionData.ProviderToDemandGetAll().Count();

                Assert.True(sumDemandToProviderAndProviderToDemand == orderGraph.CountEdges(),
                    $"Should be equal size: sumDemandToProviderAndProviderToDemand " +
                    $"{sumDemandToProviderAndProviderToDemand} and  sumValuesOfOrderGraph {orderGraph.CountEdges()}");
        }

        /**
         * Assumptions:
         * - IDemand:   T_CustomerOrderPart (COP), T_ProductionOrderBom (PrOB), T_StockExchange (SE:I)
         * - IProvider: T_PurchaseOrderPart (PuOP), T_ProductionOrder (PrO),    T_StockExchange (SE:W)
         *
         * Verifies that,
         * for demand (parent) --> provider (child) direction following takes effect:
         * - COP  --> SE:W
         * - PrOB --> SE:W
         * - SE:I --> PuOP | PrO
         *
         * for provider (parent) --> demand (child) direction following takes effect:
         * - PuOP --> NONE
         * - PrO  --> PrOB
         * - SE:W --> SE:I
         *
         * where SE:I = StockExchangeDemand
         * and SE:W = StockExchangeProvider
         * TODO: remove StockExchangeType from T_StockExchange since it's exactly specified by Withdrawal/Insert
         */
        [Fact]
        public void TestEdgeTypes()
        {
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

            // build orderGraph up
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);
            IGraph<INode> orderGraph = new OrderGraph(dbTransactionData);
            
            // verify edgeTypes
            foreach (var customerOrderPart in dbMasterDataCache.T_CustomerOrderPartGetAll().GetAll()
            )
            {
                orderGraph.TraverseDepthFirst((INode parentNode, List<INode> childNodes) =>
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

        [Fact]
        public void TestOrderGraphStaysTheSame()
        {
            string orderGraphFileName = $"../../../Test/Ordergraphs/ordergraph_cop_{ORDER_QUANTITY}_lotsize_{DEFAULT_LOT_SIZE}.txt";
            
            // build orderGraph up
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);
            IGraph<INode> orderGraph = new OrderGraph(dbTransactionData);
            
            // for initial creating of the file
            string expectedOrderGraph = File.ReadAllText(orderGraphFileName, Encoding.UTF8);
            string actualOrderGraph = orderGraph.ToString();
            bool graphStaysTheSame = expectedOrderGraph.Equals(actualOrderGraph);
            
            // for debugging
            if (!graphStaysTheSame)
            {
                List<string> lostLines = new List<string>();
                List<string> newLines = new List<string>();
                
                List<string> expectedFileLines = File.ReadLines(orderGraphFileName).ToList();
                File.WriteAllText(orderGraphFileName, actualOrderGraph, Encoding.UTF8);
                List<string> actualFileLines = File.ReadLines(orderGraphFileName).ToList();
                
                // remove ids
                expectedFileLines = removeIdsFromOrderGraph(expectedFileLines);
                string expectedFile = String.Join("\r\n", expectedFileLines);
                actualFileLines = removeIdsFromOrderGraph(actualFileLines);
                string actualFile = String.Join("\r\n", actualFileLines);
                
                // get lost lines
                foreach (var line in expectedFileLines)
                {
                    int expectedCount = CountSubString(expectedFile, line);
                    int actualCount = CountSubString(actualFile, line);
                    
                        if (actualCount < expectedCount)
                        {
                            for (int i = actualCount; i < expectedCount; i++)
                            {
                                lostLines.Add(line);
                            }
                        }
                        if (actualCount > expectedCount)
                        {
                            for (int i = expectedCount; i < actualCount; i++)
                            {
                                newLines.Add(line);
                            }
                        }
                }

                if (lostLines.Any())
                {
                    File.WriteAllLines(orderGraphFileName + "_lostLines.txt", lostLines, Encoding.UTF8);
                }
                if (newLines.Any())
                {
                    File.WriteAllLines(orderGraphFileName+ "_newlines.txt", newLines, Encoding.UTF8);
                }
                
                
            }
            
            Assert.True(graphStaysTheSame, "OrderGraph has changed.");
        }

        private int CountSubString(string source, string substring)
        {
            int count = 0, n = 0;

            if(substring != "")
            {
                while ((n = source.IndexOf(substring, n, StringComparison.InvariantCulture)) != -1)
                {
                    n += substring.Length;
                    ++count;
                }
            }

            return count;
        }

        private List<string> removeIdsFromOrderGraph(List<string> orderGraphLines)
        {
            List<string> orderGraphWithoutIds = new List<string>();
            foreach (var orderGraphLine in orderGraphLines)
            {
                string newString = "";
                string[] splitted = orderGraphLine.Split("->");
                if (splitted.Length == 2)
                {
                    newString += splitted[0].Substring(7, splitted[0].Length - 7);
                    newString += " -> ";
                    newString += splitted[1].Substring(8, splitted[1].Length - 8);
                    
                    orderGraphWithoutIds.Add(newString);
                }
            }

            return orderGraphWithoutIds;
        }
    }
}