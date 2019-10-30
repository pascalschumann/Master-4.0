using System.Linq;
using Xunit;
using Zpp.Test.Configuration;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.Test.Unit_Tests
{
    public class TestDirectedGraph
    {
        [Fact]
        public void TestAsString()
        {
            INode[] nodes = EntityFactory.CreateDummyNodes(7);
            IDirectedGraph<INode> directedGraph = CreateBinaryDirectedGraph(nodes);
            Assert.True(directedGraph.AsString() != null,
                "AsString() must work in unit tests without a database.");
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
                Assert.True(leafs.Contains(nodes[i]), $"Leafs do not contain node {nodes[i]}.");
            }
        }

        [Fact]
        public void TestGetRoots()
        {
            INode[] nodes = EntityFactory.CreateDummyNodes(7);
            IDirectedGraph<INode> directedGraph = CreateBinaryDirectedGraph(nodes);
            INodes roots = directedGraph.GetRootNodes();

            Assert.True(roots != null, "There should be roots in the graph.");

            Assert.True(roots.Contains(nodes[0]), $"Leafs do not contain node {nodes[0]}.");
            Assert.True(roots.Count() == 1, "Roots must contain exact one node.");
        }

        [Fact]
        public void TestGetSuccessorNodes()
        {
            INode[] nodes = EntityFactory.CreateDummyNodes(7);
            IDirectedGraph<INode> directedGraph = CreateBinaryDirectedGraph(nodes);
            INodes leafs = directedGraph.GetLeafNodes();
            foreach (var node in nodes)
            {
                INodes successors = directedGraph.GetSuccessorNodes(node);
                bool isLeaf = leafs.Contains(node);
                if (isLeaf)
                {
                    Assert.True(successors == null, "A leaf cannot have successors.");
                }
                else
                {
                    Assert.True(successors != null, "A non-leaf MUST have successors.");
                }
            }
        }

        [Fact]
        public void TestGetPredecessorNodes()
        {
            INode[] nodes = EntityFactory.CreateDummyNodes(7);
            IDirectedGraph<INode> directedGraph = CreateBinaryDirectedGraph(nodes);
            INodes roots = directedGraph.GetRootNodes();
            foreach (var node in nodes)
            {
                INodes predecessors = directedGraph.GetPredecessorNodes(node);
                bool isRoot = roots.Contains(node);
                if (isRoot)
                {
                    Assert.True(predecessors == null, "A root cannot have predecessors.");
                }
                else
                {
                    Assert.True(predecessors != null, "A non-root MUST have predecessors.");
                }
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