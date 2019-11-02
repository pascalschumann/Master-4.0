using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Microsoft.EntityFrameworkCore.Internal;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Mrp2.impl.Scheduling.impl.JobShopScheduler;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.ZppSimulator.impl;

namespace Zpp.DataLayer.impl
{
    public class Aggregator : IAggregator
    {
        private readonly IDbMasterDataCache _dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();

        private readonly DemandToProviderGraph _demandToProviderGraph;
        private readonly OrderOperationGraph _orderOperationGraph;

        private readonly IDbTransactionData _dbTransactionData;

        public Aggregator(IDbTransactionData dbTransactionData,
            DemandToProviderGraph demandToProviderGraph, OrderOperationGraph orderOperationGraph)
        {
            _dbTransactionData = dbTransactionData;
            _demandToProviderGraph = demandToProviderGraph;
            _orderOperationGraph = orderOperationGraph;
        }

        public ProductionOrderBoms GetProductionOrderBomsOfProductionOrder(
            ProductionOrder productionOrder)
        {
            throw new System.NotImplementedException();
        }

        public List<Resource> GetResourcesByResourceSkillId(Id resourceSkillId)
        {
            var setupIds = _dbMasterDataCache.M_ResourceSetupGetAll()
                .Where(x => x.ResourceSkillId.Equals(resourceSkillId.GetValue()))
                .Select(i => i.ResourceId);
            var resources = _dbMasterDataCache.ResourceGetAll()
                .Where(x => setupIds.Contains(x.GetValue().Id)).ToList();
            return resources;
        }

        public List<ProductionOrderOperation> GetProductionOrderOperationsOfProductionOrder(
            ProductionOrder productionOrder)
        {
            return GetProductionOrderOperationsOfProductionOrder(productionOrder.GetId());
        }

        public List<ProductionOrderOperation> GetProductionOrderOperationsOfProductionOrder(
            Id productionOrderId)
        {
            INodes successorNodes = _orderOperationGraph.GetSuccessorNodes(productionOrderId);
            if (successorNodes.Any() == false)
            {
                return null;
            }

            return successorNodes.Select(x => (ProductionOrderOperation) x.GetEntity()).ToList();
        }

        public ProductionOrderBom GetAnyProductionOrderBomByProductionOrderOperation(
            ProductionOrderOperation productionOrderOperation)
        {
            T_ProductionOrderBom productionOrderBom = _dbTransactionData.ProductionOrderBomGetAll()
                .GetAllAs<T_ProductionOrderBom>().Find(x =>
                    x.ProductionOrderOperationId.Equals(productionOrderOperation.GetId()
                        .GetValue()));
            if (productionOrderBom == null)
            {
                throw new MrpRunException(
                    "How could an productionOrderOperation without an T_ProductionOrderBom exists?");
            }

            return new ProductionOrderBom(productionOrderBom);
        }

        public Providers GetAllChildProvidersOf(Demand demand)
        {
            INodes successors = _demandToProviderGraph.GetSuccessorNodes(demand.GetId());
            if (successors == null)
            {
                return null;
            }

            return new Providers(successors.Select(x => (Provider) x.GetEntity()));
        }

        public Providers GetAllParentProvidersOf(Demand demand)
        {
            INodes predecessors = _demandToProviderGraph.GetPredecessorNodes(demand.GetId());
            if (predecessors == null)
            {
                return null;
            }

            return new Providers(predecessors.Select(x => (Provider) x.GetEntity()));
        }

        public List<Provider> GetProvidersForInterval(DueTime from, DueTime to)
        {
            var providers = _dbTransactionData.StockExchangeProvidersGetAll();
            var currentProviders = providers.GetAll().FindAll(x =>
                x.GetStartTimeBackward().GetValue() >= from.GetValue() &&
                x.GetStartTimeBackward().GetValue() <= to.GetValue());
            return currentProviders;
        }

        public Demands GetAllParentDemandsOf(Provider provider)
        {
            INodes predecessors = _demandToProviderGraph.GetPredecessorNodes(provider.GetId());
            if (predecessors == null)
            {
                return null;
            }

            return new Demands(predecessors.Select(x => (Demand) x.GetEntity()));
        }

        public Demands GetAllChildDemandsOf(Provider provider)
        {
            INodes successors = _demandToProviderGraph.GetSuccessorNodes(provider.GetId());
            if (successors == null)
            {
                return null;
            }

            return new Demands(successors.Select(x => (Demand) x.GetEntity()));
        }

        public Providers GetAllChildStockExchangeProvidersOf(ProductionOrderOperation operation)
        {
            INodes successors = _orderOperationGraph.GetSuccessorNodes(operation.GetId());
            if (successors == null)
            {
                return null;
            }

            Providers providers = new Providers();
            foreach (var successor in successors)
            {
                if (successor.GetEntity().GetType() == typeof(StockExchangeProvider))
                {
                    providers.Add((StockExchangeProvider)successor.GetEntity());
                }
                else if (successor.GetEntity().GetType() == typeof(ProductionOrderOperation))
                {
                    // pass
                }
                else
                {
                    throw new MrpRunException(
                        "A child of an operation can only be an operation or " +
                        "a StockExchangeProvider");
                }
            }

            if (providers.Any() == false)
            {
                return null;
            }

            return providers;
        }

        public DueTime GetEarliestPossibleStartTimeOf(
            ProductionOrderOperation productionOrderOperation)
        {
            DueTime maximumOfEarliestStartTimes = null;
            Providers providers = ZppConfiguration.CacheManager.GetAggregator()
                .GetAllChildStockExchangeProvidersOf(productionOrderOperation);

            foreach (var stockExchangeProvider in providers)
            {
                DueTime earliestStartTime = productionOrderOperation.GetStartTimeBackward();
                if (earliestStartTime.IsGreaterThanOrEqualTo(stockExchangeProvider
                    .GetStartTimeBackward()))
                {
                    earliestStartTime = stockExchangeProvider.GetStartTimeBackward();
                }
                else
                {
                    throw new MrpRunException(
                        "A provider of a demand cannot have a later dueTime.");
                }

                Demands stockExchangeDemands = ZppConfiguration.CacheManager.GetAggregator()
                    .GetAllChildDemandsOf(stockExchangeProvider);
                if (stockExchangeDemands.Any() == false)
                    // StockExchangeProvider has no childs (stockExchangeDemands),
                    // take that from stockExchangeProvider
                {
                    DueTime childDueTime = stockExchangeProvider.GetStartTimeBackward();
                    if (childDueTime.IsGreaterThan(earliestStartTime))
                    {
                        earliestStartTime = childDueTime;
                    }
                }
                else
                    // StockExchangeProvider has childs (stockExchangeDemands)
                {
                    foreach (var stockExchangeDemand in stockExchangeDemands)
                    {
                        DueTime stockExchangeDemandDueTime =
                            stockExchangeDemand.GetStartTimeBackward();
                        if (stockExchangeDemandDueTime.IsGreaterThan(earliestStartTime))
                        {
                            earliestStartTime = stockExchangeDemandDueTime;
                        }
                    }
                }

                if (maximumOfEarliestStartTimes == null ||
                    earliestStartTime.IsGreaterThan(maximumOfEarliestStartTimes))
                {
                    maximumOfEarliestStartTimes = earliestStartTime;
                }
            }

            return maximumOfEarliestStartTimes;
        }


        public Demands GetUnsatisifedCustomerOrderParts()
        {
            Demands unsatisifedCustomerOrderParts = new Demands();

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            Demands customerOrderParts = dbTransactionData.CustomerOrderPartGetAll();

            foreach (var customerOrderPart in customerOrderParts)
            {
                if (aggregator.ExistsInDemandToProviderGraph(customerOrderPart.GetId()) == false)
                {
                    unsatisifedCustomerOrderParts.Add(customerOrderPart);
                }
            }

            return unsatisifedCustomerOrderParts;
        }

        public DemandOrProviders GetDemandsOrProvidersWhereStartTimeIsWithinInterval(
            SimulationInterval simulationInterval, DemandOrProviders demandOrProviders)
        {
            // startTime within interval
            return new DemandOrProviders(demandOrProviders.GetAll().Where(x =>
                simulationInterval.IsWithinInterval(x.GetStartTimeBackward())));
        }

        public DemandOrProviders GetDemandsOrProvidersWhereEndTimeIsWithinIntervalOrBefore(
            SimulationInterval simulationInterval, DemandOrProviders demandOrProviders)
        {
            // endTime within interval
            return new DemandOrProviders(demandOrProviders.GetAll().Where(x =>
            {
                DueTime endTime = x.GetEndTimeBackward();
                return simulationInterval.IsWithinInterval(endTime) ||
                       simulationInterval.IsBeforeInterval(endTime);
            }));
        }

        /**
         * DemandToProvider
         */
        public IEnumerable<ILinkDemandAndProvider> GetArrowsTo(Provider provider)
        {
            return _demandToProviderGraph.GetEdgesTo(provider.GetId());
        }

        /**
         * ProviderToDemand
         */
        public IEnumerable<ILinkDemandAndProvider> GetArrowsFrom(Provider provider)
        {
            return _demandToProviderGraph.GetEdgesFrom(provider.GetId());
        }

        /**
         * ProviderToDemand
         */
        public IEnumerable<ILinkDemandAndProvider> GetArrowsTo(Demand demand)
        {
            return _demandToProviderGraph.GetEdgesTo(demand.GetId());
        }

        /**
         * DemandToProvider
         */
        public IEnumerable<ILinkDemandAndProvider> GetArrowsFrom(Demand demand)
        {
            return _demandToProviderGraph.GetEdgesFrom(demand.GetId());
        }

        public IEnumerable<ILinkDemandAndProvider> GetArrowsTo(Providers providers)
        {
            List<ILinkDemandAndProvider> list = new List<ILinkDemandAndProvider>();
            foreach (var provider in providers)
            {
                list.AddRange(GetArrowsTo(provider));
            }

            return list;
        }

        public IEnumerable<ILinkDemandAndProvider> GetArrowsFrom(Providers providers)
        {
            List<ILinkDemandAndProvider> list = new List<ILinkDemandAndProvider>();
            foreach (var provider in providers)
            {
                list.AddRange(GetArrowsFrom(provider));
            }

            return list;
        }

        public IEnumerable<ILinkDemandAndProvider> GetArrowsTo(Demands demands)
        {
            List<ILinkDemandAndProvider> list = new List<ILinkDemandAndProvider>();
            foreach (var demand in demands)
            {
                list.AddRange(GetArrowsTo(demand));
            }

            return list;
        }

        public IEnumerable<ILinkDemandAndProvider> GetArrowsFrom(Demands demands)
        {
            List<ILinkDemandAndProvider> list = new List<ILinkDemandAndProvider>();
            foreach (var demand in demands)
            {
                list.AddRange(GetArrowsFrom(demand));
            }

            return list;
        }


        /**
         * Arrow equals DemandToProvider and ProviderToDemand
         */
        public List<ILinkDemandAndProvider> GetArrowsToAndFrom(Provider provider)
        {
            List<ILinkDemandAndProvider>
                demandAndProviderLinks = new List<ILinkDemandAndProvider>();

            IEnumerable<ILinkDemandAndProvider> demandToProviders = GetArrowsTo(provider);
            IEnumerable<ILinkDemandAndProvider> providerToDemands = GetArrowsFrom(provider);

            demandAndProviderLinks.AddRange(demandToProviders);
            demandAndProviderLinks.AddRange(providerToDemands);

            return demandAndProviderLinks;
        }

        /**
         * Arrow equals DemandToProvider and ProviderToDemand
         */
        public List<ILinkDemandAndProvider> GetArrowsToAndFrom(Demand demand)
        {
            List<ILinkDemandAndProvider>
                demandAndProviderLinks = new List<ILinkDemandAndProvider>();

            IEnumerable<ILinkDemandAndProvider> demandToProviders = GetArrowsTo(demand);
            IEnumerable<ILinkDemandAndProvider> providerToDemands = GetArrowsFrom(demand);
            demandAndProviderLinks.AddRange(demandToProviders);
            demandAndProviderLinks.AddRange(providerToDemands);

            return demandAndProviderLinks;
        }

        public List<ILinkDemandAndProvider> GetArrowsToAndFrom(IDemandOrProvider demandOrProvider)
        {
            if (demandOrProvider is Demand)
            {
                return GetArrowsToAndFrom((Demand) demandOrProvider);
            }
            else if (demandOrProvider is Provider)
            {
                return GetArrowsToAndFrom((Provider) demandOrProvider);
            }
            else
            {
                throw new MrpRunException("This type is not expected.");
            }
        }

        public List<ProductionOrderOperation> GetAllOperationsOnResource(M_Resource resource)
        {
            return _dbTransactionData.ProductionOrderOperationGetAll().GetAll()
                .Where(x => x.GetMachineId().GetValue().Equals(resource.Id)).ToList();
        }

        public bool ExistsInDemandToProviderGraph(Id nodeId)
        {
            return _demandToProviderGraph.Contains(nodeId);
        }

        internal OrderOperationGraph GetOrderOperationGraph()
        {
            return _orderOperationGraph;
        }

        internal DemandToProviderGraph GetDemandToProviderGraph()
        {
            return _demandToProviderGraph;
        }
    }
}