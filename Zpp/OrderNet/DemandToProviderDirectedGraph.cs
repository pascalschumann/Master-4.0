using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Xunit;
using Zpp.DemandDomain;
using Zpp.GraphicalRepresentation;
using Zpp.MachineDomain;
using Zpp.ProviderDomain;
using Zpp.Utils;

namespace Zpp
{
    public class DemandToProviderDirectedGraph : IDirectedGraph<INode>
    {
        protected readonly Dictionary<INode, List<IEdge>> _adjacencyList =
            new Dictionary<INode, List<IEdge>>();

        protected readonly IDbTransactionData _dbTransactionData;

        protected DemandToProviderDirectedGraph()
        {
        }

        public DemandToProviderDirectedGraph(IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;

            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll().GetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(new Id(demandToProvider.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(demandToProvider.ProviderId));
                Assert.True(demand != null || provider != null,
                    "Demand/Provider should not be null.");
                INode fromNode = new Node(demand, demandToProvider.GetDemandId());
                INode toNode = new Node(provider, demandToProvider.GetProviderId());
                AddEdge(fromNode, new Edge(demandToProvider, fromNode, toNode));
            }

            foreach (var providerToDemand in dbTransactionData.ProviderToDemandGetAll().GetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(new Id(providerToDemand.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(providerToDemand.ProviderId));
                Assert.True(demand != null || provider != null,
                    "Demand/Provider should not be null.");

                INode fromNode = new Node(provider, providerToDemand.GetProviderId());
                INode toNode = new Node(demand, providerToDemand.GetDemandId());
                AddEdge(fromNode,
                    new Edge(providerToDemand.ToDemandToProvider(), fromNode, toNode));
            }
        }

        public List<INode> GetSuccessorNodes(INode tailNode)
        {
            if (!_adjacencyList.ContainsKey(tailNode))
            {
                return null;
            }

            return _adjacencyList[tailNode].Select(x => x.GetHeadNode()).ToList();
        }

        public List<INode> GetPredecessorNodes(INode headNode)
        {
            List<INode> predecessorNodes = new List<INode>();
            foreach (var edge in GetAllEdgesTowardsHeadNode(headNode))
            {
                predecessorNodes.Add(edge.GetTailNode());
            }

            return predecessorNodes;
        }

        public void AddEdges(INode fromNode, List<IEdge> edges)
        {
            if (!_adjacencyList.ContainsKey(fromNode))
            {
                _adjacencyList.Add(fromNode, edges);
                return;
            }

            _adjacencyList[fromNode].AddRange(edges);
        }

        public void AddEdge(INode fromNode, IEdge edge)
        {
            if (!_adjacencyList.ContainsKey(fromNode))
            {
                _adjacencyList.Add(fromNode, new List<IEdge>());
            }

            _adjacencyList[fromNode].Add(edge);
        }

        public int CountEdges()
        {
            return GetAllHeadNodes().Count;
        }

        public List<IEdge> GetAllEdgesFromTailNode(INode tailNode)
        {
            if (_adjacencyList.ContainsKey(tailNode) == false)
            {
                return null;
            }

            return _adjacencyList[tailNode];
        }

        public List<IEdge> GetAllEdgesTowardsHeadNode(INode headNode)
        {
            List<IEdge> edgesTowardsHeadNode = new List<IEdge>();
            foreach (var tailNode in GetAllTailNodes())
            {
                foreach (var edge in _adjacencyList[tailNode])
                {
                    if (edge.GetHeadNode().Equals(headNode))
                    {
                        edgesTowardsHeadNode.Add(edge);
                    }
                }
            }

            if (edgesTowardsHeadNode.Any() == false)
            {
                return null;
            }

            return edgesTowardsHeadNode;
        }

        public override string ToString()
        {
            string mystring = "";
            foreach (var fromNode in GetAllTailNodes())
            {
                foreach (var edge in GetAllEdgesFromTailNode(fromNode))
                {
                    // <Type>, <Menge>, <ItemName> and on edges: <Menge>
                    Quantity quantity = null;
                    if (edge.GetDemandToProvider() != null)
                    {
                        quantity = new Quantity(edge.GetDemandToProvider().Quantity);
                    }

                    mystring +=
                        $"\"{fromNode.GetId()};{fromNode.GetGraphizString(_dbTransactionData)}\" -> " +
                        $"\"{edge.GetHeadNode().GetId()};{edge.GetHeadNode().GetGraphizString(_dbTransactionData)}\"";
                    // if (quantity.IsNull() == false)
                    if (quantity != null && quantity.IsNull() == false)
                    {
                        mystring += $" [ label=\" {quantity}\" ]";
                    }

                    mystring += ";" + Environment.NewLine;
                }
            }

            return mystring;
        }

        public List<INode> GetAllHeadNodes()
        {
            List<INode> toNodes = new List<INode>();

            foreach (var edges in _adjacencyList.Values.ToList())
            {
                foreach (var edge in edges)
                {
                    toNodes.Add(edge.GetHeadNode());
                }
            }

            return toNodes;
        }

        // 
        // TODO: Switch this to iterative depth search (with dfs limit default set to max depth of given truck examples)
        ///
        /// <summary>
        ///     A depth-first-search (DFS) traversal of given tree
        /// </summary>
        /// <param name="graph">to traverse</param>
        /// <returns>
        ///    The List of the traversed nodes in exact order
        /// </returns>
        public List<INode> TraverseDepthFirst(Action<INode, List<INode>, List<INode>> action,
            CustomerOrderPart startNode)
        {
            var stack = new Stack<INode>();

            Dictionary<INode, bool> discovered = new Dictionary<INode, bool>();
            List<INode> traversed = new List<INode>();

            stack.Push(startNode);
            INode parentNode;

            while (stack.Any())
            {
                INode poppedNode = stack.Pop();

                // init dict if node not yet exists
                if (!discovered.ContainsKey(poppedNode))
                {
                    discovered[poppedNode] = false;
                }

                // if node is not discovered
                if (!discovered[poppedNode])
                {
                    traversed.Add(poppedNode);
                    discovered[poppedNode] = true;
                    List<INode> childNodes = GetSuccessorNodes(poppedNode);
                    action(poppedNode, childNodes, traversed);

                    if (childNodes != null)
                    {
                        foreach (INode node in childNodes)
                        {
                            stack.Push(node);
                        }
                    }
                }
            }

            return traversed;
        }

        public List<INode> GetAllTailNodes()
        {
            return _adjacencyList.Keys.ToList();
        }

        public List<INode> GetAllUniqueNode()
        {
            List<INode> fromNodes = GetAllTailNodes();
            List<INode> toNodes = GetAllHeadNodes();
            IStackSet<INode> uniqueNodes = new StackSet<INode>();
            uniqueNodes.PushAll(fromNodes);
            uniqueNodes.PushAll(toNodes);

            return uniqueNodes.GetAll();
        }

        public GanttChart GetAsGanttChart(IDbTransactionData dbTransactionData)
        {
            GanttChart ganttChart = new GanttChart();

            foreach (var node in GetAllUniqueNode())
            {
                if (node.GetNodeType().Equals(NodeType.Demand))
                {
                    Demand demand = (Demand) node.GetEntity();
                    GanttChartBar ganttChartBar = new GanttChartBar()
                    {
                        article = demand.GetArticle().Name,
                        articleId = demand.GetArticle().Id.ToString(),
                        end = demand.GetDueTime(dbTransactionData).ToString(),
                    };
                    if (demand.GetStartTime(dbTransactionData) != null)
                    {
                        ganttChartBar.start = demand.GetStartTime(dbTransactionData).ToString();
                    }

                    if (demand.GetType() == typeof(ProductionOrderBom))
                    {
                        ProductionOrderBom productionOrderBom =
                            (ProductionOrderBom) demand.GetEntity();

                        ProductionOrderOperation productionOrderOperation =
                            productionOrderBom.GetProductionOrderOperation(dbTransactionData);
                        if (productionOrderOperation != null)
                        {
                            ganttChartBar.operation = productionOrderOperation.GetValue().Name;
                            ganttChartBar.operationId =
                                productionOrderOperation.GetValue().Id.ToString();
                        }
                    }

                    ganttChart.AddGanttChartBar(ganttChartBar);
                }
                else if (node.GetNodeType().Equals(NodeType.Provider))
                {
                    Provider provider = (Provider) node.GetEntity();
                    GanttChartBar ganttChartBar = new GanttChartBar()
                    {
                        article = provider.GetArticle().Name,
                        articleId = provider.GetArticle().Id.ToString(),
                        end = provider.GetDueTime(dbTransactionData).ToString()
                    };
                    if (provider.GetStartTime(dbTransactionData) != null)
                    {
                        ganttChartBar.start = provider.GetStartTime(dbTransactionData).ToString();
                    }

                    ganttChart.AddGanttChartBar(ganttChartBar);
                }
            }

            return ganttChart;
        }

        public void RemoveNode(INode node)
        {
            List<IEdge> edgesTowardsNode = GetAllEdgesTowardsHeadNode(node);
            List<IEdge> edgesFromNode = GetAllEdgesFromTailNode(node);
            RemoveAllEdgesFromTailNode(node);
            RemoveAllEdgesTowardsHeadNode(node);

            // node is NOT start node AND NOT leaf node
            if (edgesTowardsNode != null && edgesFromNode != null)
            {
                foreach (var edgeTowardsNode in edgesTowardsNode)

                {
                    foreach (var edgeFromNode in edgesFromNode)
                    {
                        AddEdge(edgeTowardsNode.GetTailNode(),
                            new Edge(edgeTowardsNode.GetTailNode(), edgeFromNode.GetHeadNode()));
                    }
                }
            }
        }

        public void RemoveAllEdgesFromTailNode(INode tailNode)
        {
            _adjacencyList.Remove(tailNode);
        }

        public void RemoveAllEdgesTowardsHeadNode(INode headNode)
        {
            foreach (var tailNode in GetAllTailNodes())
            {
                List<IEdge> edgesToDelete = new List<IEdge>();
                foreach (var edge in _adjacencyList[tailNode])
                {
                    if (edge.GetHeadNode().Equals(headNode))
                    {
                        edgesToDelete.Add(edge);
                    }
                }

                foreach (var edgeToDelete in edgesToDelete)
                {
                    _adjacencyList[tailNode].Remove(edgeToDelete);
                }
            }
        }
    }
}