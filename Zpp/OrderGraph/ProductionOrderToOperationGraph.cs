using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Enums;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.ProductionManagement.ProductionTypes;
using Zpp.OrderGraph;
using Zpp.Utils;

namespace Zpp.OrderGraph
{
    public class ProductionOrderToOperationGraph: ITwoDimensionalGraph<INode>
    {
        private readonly IDbMasterDataCache _dbMasterDataCache;
        private readonly IAggregator _aggregator;
        private readonly IDbTransactionData _dbTransactionData;
        private readonly IDirectedGraph<INode> _productionOrderGraph;
        private readonly ProductionOrderOperationGraphsAsDictionary _productionOrderOperationGraphsAsDictionary;

        public ProductionOrderToOperationGraph(IDbMasterDataCache dbMasterDataCache, 
                              IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;
            _dbMasterDataCache = dbMasterDataCache;
            _aggregator = dbTransactionData.GetAggregator();
            _productionOrderGraph = new ProductionOrderDirectedGraph(_dbTransactionData, false);
            _productionOrderOperationGraphsAsDictionary = new ProductionOrderOperationGraphsAsDictionary();
            Init();
        }

        private void Init()
        {
            foreach (var productionOrder in _dbTransactionData.ProductionOrderGetAll())
            {
                IDirectedGraph<INode> productionOrderOperationGraph =
                    new ProductionOrderOperationDirectedGraph(_dbTransactionData,
                        (ProductionOrder)productionOrder);
                _productionOrderOperationGraphsAsDictionary.Add((ProductionOrder)productionOrder,
                    productionOrderOperationGraph);
            }
        }

        public void RemoveOperation(ProductionOrderOperation operation)
        {

            var productionOrder = operation.GetProductionOrder(_dbTransactionData);
            var productionOrderOperationGraph =
                (ProductionOrderOperationDirectedGraph)_productionOrderOperationGraphsAsDictionary[productionOrder];

            // prepare for next round
            productionOrderOperationGraph.RemoveNode(operation);
            productionOrderOperationGraph
                    // TODO Naming ?
                .RemoveProductionOrdersWithNoProductionOrderOperationsFromProductionOrderGraph(
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
                    _productionOrderOperationGraphsAsDictionary[(ProductionOrder) productionOrder.GetEntity()];
                var productionOrderOperationLeafsOfProductionOrder =
                    productionOrderOperationGraph.GetLeafNodes();

                allLeafs.PushAll(productionOrderOperationLeafsOfProductionOrder);
            }

            return allLeafs;
        }

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
            if (node.GetType() == typeof(ProductionOrder))
            {
                
                return _productionOrderGraph.GetPredecessorNodes(node);
            }
            else if (node.GetType() == typeof(ProductionOrderOperation))
            {
                ProductionOrderOperation productionOrderOperation = (ProductionOrderOperation)node.GetEntity();
                ProductionOrder productionOrder = productionOrderOperation.GetProductionOrder(_dbTransactionData);
                ProductionOrderOperationDirectedGraph productionOrderOperationGraph =
                    (ProductionOrderOperationDirectedGraph) _productionOrderOperationGraphsAsDictionary[
                        productionOrder];
                return productionOrderOperationGraph.GetPredecessorNodes(productionOrderOperation);
            }
            else
            {
                throw  new MrpRunException("Another graph should not possible.");
            }
        }
    }
}