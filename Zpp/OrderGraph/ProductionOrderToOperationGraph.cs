using Master40.DB.Enums;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.ProductionManagement.ProductionTypes;
using Zpp.OrderGraph;
using Zpp.Utils;

namespace Zpp.OrderGraph
{
    public class ProductionOrderToOperationGraph: IProductionOrderToOperationGraph<INode>
    {
        private readonly IDbMasterDataCache _dbMasterDataCache;
        private readonly IAggregator _aggregator;
        private readonly IDbTransactionData _dbTransactionData;
        private readonly IDirectedGraph<INode> _productionOrderGraph;
        private readonly ProductionOrderOperationGraphsAsDictionary _productionOrderToOperationGraph;
        


        public ProductionOrderToOperationGraph(IDbMasterDataCache dbMasterDataCache, 
                              IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;
            _dbMasterDataCache = dbMasterDataCache;
            _aggregator = dbTransactionData.GetAggregator();
            _productionOrderGraph = new ProductionOrderDirectedGraph(_dbTransactionData, false);
            _productionOrderToOperationGraph = new ProductionOrderOperationGraphsAsDictionary();
            Init();
        }

        private void Init()
        {
            foreach (var productionOrder in _dbTransactionData.ProductionOrderGetAll())
            {
                IDirectedGraph<INode> productionOrderOperationGraph =
                    new ProductionOrderOperationDirectedGraph(_dbTransactionData,
                        (ProductionOrder)productionOrder);
                _productionOrderToOperationGraph.Add((ProductionOrder)productionOrder,
                    productionOrderOperationGraph);
            }
        }

        public void Remove(ProductionOrderOperation operation)
        {

            var productionOrder = operation.GetProductionOrder(_dbTransactionData);
            var productionOrderOperationGraph =
                (ProductionOrderOperationDirectedGraph)_productionOrderToOperationGraph[productionOrder];
            
            productionOrderOperationGraph.RemoveNode(operation);
            productionOrderOperationGraph
                .RemoveProductionOrdersWithNoProductionOrderOperations(
                    _productionOrderGraph, productionOrder);
        }

        public void Remove(ProductionOrder productionOrder)
        {
            var productionOrderOperationGraph =
                (ProductionOrderOperationDirectedGraph)_productionOrderToOperationGraph[productionOrder];
            
            productionOrderOperationGraph
                .RemoveProductionOrdersWithNoProductionOrderOperations(
                    _productionOrderGraph, productionOrder);
        }

        /// <summary>
        /// returns the mature cherry's
        /// </summary>
        /// <returns></returns>
        public StackSet<INode> GetLeafs()
        {
            INodes leafNodes = _productionOrderGraph.GetLeafNodes(); 
            if (leafNodes == null)
            {
                return null;
            }

            StackSet<INode> allLeafs = new StackSet<INode>();
            foreach (var productionOrder in leafNodes)
            {
                var productionOrderOperationGraph =
                    _productionOrderToOperationGraph[(ProductionOrder) productionOrder.GetEntity()];
                var productionOrderOperationLeafsOfProductionOrder =
                    productionOrderOperationGraph.GetLeafNodes();

                allLeafs.PushAll(productionOrderOperationLeafsOfProductionOrder);
            }

            return allLeafs;
        }

        // TODO: this method should not be in this class
        public void WithdrawMaterialsFromStock(ProductionOrderOperation operation, long time)
        {
            var productionOrderBoms = _dbTransactionData.GetAggregator().GetAllProductionOrderBomsBy(operation);
            foreach (var productionOrderBom in productionOrderBoms)
            {
                var providers = _dbTransactionData.GetAggregator().GetAllChildProvidersOf(productionOrderBom);
                foreach (var provider in providers)
                {
                    var stockExchangeProvider = (StockExchangeProvider)provider;
                    var stockExchange = (T_StockExchange)stockExchangeProvider.ToIProvider();
                    stockExchange.State = State.Finished;
                    stockExchange.Time = (int)time;
                }
            }
        }

        // TODO: this method should not be in this class
        public void InsertMaterialsIntoStock(ProductionOrderOperation operation, long time)
        {
            var productionOrderBom = _dbTransactionData.GetAggregator()
                .GetAnyProductionOrderBomByProductionOrderOperation(operation);


            var demands = _aggregator.GetAllParentDemandsOf(productionOrderBom.GetProductionOrder(_dbTransactionData));
            foreach (var demand in demands)
            {
                var stockExchangeProvider = (StockExchangeDemand) demand;
                var stockExchange = (T_StockExchange) stockExchangeProvider.ToIDemand();
                stockExchange.State = State.Finished;
                stockExchange.Time = (int) time;
            }
        }
        
        public INodes GetPredecessors(INode node)
        {
            INode entity = node.GetEntity();
            
            if (entity.GetType() == typeof(ProductionOrder))
            {
                
                return _productionOrderGraph.GetPredecessorNodes(entity);
            }
            else if (entity.GetType() == typeof(ProductionOrderOperation))
            {
                ProductionOrderOperation productionOrderOperation = (ProductionOrderOperation)entity.GetEntity();
                ProductionOrder productionOrder = productionOrderOperation.GetProductionOrder(_dbTransactionData);
                ProductionOrderOperationDirectedGraph productionOrderOperationGraph =
                    (ProductionOrderOperationDirectedGraph) _productionOrderToOperationGraph[
                        productionOrder];
                return productionOrderOperationGraph.GetPredecessorNodes(productionOrderOperation);
            }
            else
            {
                throw  new MrpRunException("Another type should not possible.");
            }
        }

        public IDirectedGraph<INode> GetInnerGraph(ProductionOrder productionOrder)
        {
            return _productionOrderToOperationGraph[productionOrder];
        }

        public void Remove(INode node)
        {
            INode entity = node.GetEntity();
            if (entity.GetEntity().GetType() == typeof(ProductionOrder))
            {
                Remove((ProductionOrder)node.GetEntity());
            }
            else if (entity.GetEntity().GetType() == typeof(ProductionOrderOperation))
            {
                Remove((ProductionOrderOperation)entity.GetEntity());
            }
            else
            {
                throw new MrpRunException("Unknown type.");
            }
        }

        public ProductionOrderOperations GetAllOperations()
        {
            ProductionOrderOperations productionOrderOperations = new ProductionOrderOperations();
            foreach (var operationGraph in _productionOrderToOperationGraph.Values)
            {
                foreach (var uniqueNode in operationGraph.GetAllUniqueNode())
                {
                    INode entity = uniqueNode.GetEntity();
                    if (entity.GetType() == typeof(ProductionOrderOperation))
                    {
                        productionOrderOperations.Add((ProductionOrderOperation)entity);    
                    }
                }
            }

            return productionOrderOperations;
        }

        public ProductionOrders GetAllProductionOrders()
        {
            ProductionOrders productionOrders = new ProductionOrders();
            foreach (var operationGraph in _productionOrderToOperationGraph.Values)
            {
                foreach (var uniqueNode in operationGraph.GetAllUniqueNode())
                {
                    INode entity = uniqueNode.GetEntity();
                    if (entity.GetType() == typeof(ProductionOrder))
                    {
                        productionOrders.Add(((ProductionOrder)entity));    
                    }
                }
            }

            return productionOrders;
        }
    }
}