using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.Test.Configuration;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.ZppSimulator;

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

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
        }

        /**
         * Verifies, that the demandToProviderGraph
         * - can be build up from DemandToProvider+ProviderToDemand table
         * - is a top down graph TODO is not done yet<br>
         * - has all demandToProvider and providerToDemand edges
         */
        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestAllEdgesAreInDemandToProviderGraph(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            // build demandToProviderGraph up
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IDirectedGraph<INode> demandToProviderGraph = new DemandToProviderGraph();

            Assert.True(demandToProviderGraph.IsEmpty() == false,
                "There are no toNodes in the demandToProviderGraph.");

            int sumDemandToProviderAndProviderToDemand =
                dbTransactionData.DemandToProviderGetAll().Count() +
                dbTransactionData.ProviderToDemandGetAll().Count();

            Assert.True(sumDemandToProviderAndProviderToDemand == demandToProviderGraph.CountEdges(),
                $"Should be equal size: sumDemandToProviderAndProviderToDemand " +
                $"{sumDemandToProviderAndProviderToDemand} and  sumValuesOfDemandToProviderGraph {demandToProviderGraph.CountEdges()}");
        }


        public static string RemoveIdsFromDemandToProviderGraph(string demandToProviderGraph)
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