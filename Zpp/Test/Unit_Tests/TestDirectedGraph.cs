using System.Linq;
using Xunit;
using Zpp.Test.Configuration;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.Test.Unit_Tests
{
    public class TestDirectedGraph : AbstractTest
    {
        [Fact]
        public void TestToString()
        {
            INode[] nodes = EntityFactory.CreateDummyNodes(7);
            IDirectedGraph<INode> directedGraph = CreateBinaryDirectedGraph(nodes);
            Assert.True(directedGraph.ToString() != null, 
                "ToString() must work in unit tests without a database.");
        }

        [Fact]
        public void TestGetLeafs()
        {
            
            INode[] nodes = EntityFactory.CreateDummyNodes(7);
            IDirectedGraph<INode> directedGraph = CreateBinaryDirectedGraph(nodes);
            INodes leafs = directedGraph.GetLeafNodes();
            
            Assert.True(leafs != null, "There should be leafs in the graph.");
            
            for (int i = 3; i < 7; i++)
            {
                Assert.True(leafs.Contains(nodes[i]), $"Leafs do not contain {nodes[i]}.");
            }
        }

        private IDirectedGraph<INode> CreateBinaryDirectedGraph(INode[] nodes)
        {
            IDirectedGraph<INode> directedGraph = new DirectedGraph();
            INode root;
            INodes leafs = new Nodes();
            for (int i = 0; i < nodes.Length; i++)
            {
                // left: 2*i + 1 , right: 2*i + 2
                int maxIndex = nodes.Length;
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                if (left < maxIndex)
                {
                    directedGraph.AddEdge(new Edge(nodes[i], nodes[left]));
                }

                if (right < maxIndex)
                {
                    directedGraph.AddEdge(new Edge(nodes[i], nodes[right]));
                }
            }

            return directedGraph;
        }
    }
}