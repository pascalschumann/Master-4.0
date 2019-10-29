using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Mrp2.impl.Scheduling.impl.JobShopScheduler;
using Zpp.Util;
using Zpp.ZppSimulator.impl;

namespace Zpp.DataLayer.impl
{
    public class Aggregator : IAggregator
    {
        private readonly IDbMasterDataCache _dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();

        private readonly IDbTransactionData _dbTransactionData;

        public Aggregator(IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;
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
            List<ProductionOrderOperation> productionOrderOperations = _dbTransactionData
                .ProductionOrderOperationGetAll()
                .Where(x => x.GetProductionOrderId().Equals(productionOrderId)).ToList();
            if (productionOrderOperations.Any() == false)
            {
                return null;
            }

            return productionOrderOperations;
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

        public ProductionOrderBoms GetAllProductionOrderBomsBy(
            ProductionOrderOperation productionOrderOperation)
        {
            List<T_ProductionOrderBom> productionOrderBoms = _dbTransactionData
                .ProductionOrderBomGetAll().GetAllAs<T_ProductionOrderBom>().FindAll(x =>
                    x.ProductionOrderOperationId.Equals(productionOrderOperation.GetId()
                        .GetValue()));
            if (productionOrderBoms == null || productionOrderBoms.Any() == false)
            {
                throw new MrpRunException(
                    $"How could an productionOrderOperation({productionOrderOperation}) without an T_ProductionOrderBom exists?");
            }

            return new ProductionOrderBoms(productionOrderBoms);
        }

        public Providers GetAllChildProvidersOf(Demand demand)
        {
            Providers providers = new Providers();
            foreach (var demandToProvider in _dbTransactionData.DemandToProviderGetAll())
            {
                if (demandToProvider.GetDemandId().Equals(demand.GetId()))
                {
                    providers.Add(
                        _dbTransactionData.ProvidersGetById(demandToProvider.GetProviderId()));
                }
            }

            return providers;
        }

        public Providers GetAllParentProvidersOf(Demand demand)
        {
            Providers providers = new Providers();
            foreach (var demandToProvider in _dbTransactionData.ProviderToDemandGetAll())
            {
                if (demandToProvider.GetDemandId().Equals(demand.GetId()))
                {
                    providers.Add(
                        _dbTransactionData.ProvidersGetById(demandToProvider.GetProviderId()));
                }
            }

            return providers;
        }

        public List<Provider> GetProvidersForInterval(DueTime from, DueTime to)
        {
            var providers = _dbTransactionData.StockExchangeProvidersGetAll();
            var currentProviders = providers.GetAll().FindAll(x =>
                x.GetStartTime().GetValue() >= from.GetValue() &&
                x.GetStartTime().GetValue() <= to.GetValue());
            return currentProviders;
        }

        public Demands GetAllParentDemandsOf(Provider provider)
        {
            Demands demands = new Demands();
            foreach (var demandToProvider in _dbTransactionData.DemandToProviderGetAll())
            {
                if (demandToProvider.GetProviderId().Equals(provider.GetId()))
                {
                    demands.Add(_dbTransactionData.DemandsGetById(demandToProvider.GetDemandId()));
                }
            }

            return demands;
        }

        public Demands GetAllChildDemandsOf(Provider provider)
        {
            Demands demands = new Demands();
            foreach (var providerToDemand in _dbTransactionData.ProviderToDemandGetAll())
            {
                if (providerToDemand.GetProviderId().Equals(provider.GetId()))
                {
                    demands.Add(_dbTransactionData.DemandsGetById(providerToDemand.GetDemandId()));
                }
            }

            return demands;
        }

        public DueTime GetEarliestPossibleStartTimeOf(ProductionOrderBom productionOrderBom)
        {
            DueTime earliestStartTime = productionOrderBom.GetStartTime();
            Providers providers = ZppConfiguration.CacheManager.GetAggregator()
                .GetAllChildProvidersOf(productionOrderBom);
            if (providers.Count() > 1)
            {
                throw new MrpRunException("A productionOrderBom can only have one provider !");
            }


            Provider stockExchangeProvider = providers.GetAny();
            if (earliestStartTime.IsGreaterThanOrEqualTo(stockExchangeProvider.GetStartTime()))
            {
                earliestStartTime = stockExchangeProvider.GetStartTime();
            }
            else
            {
                throw new MrpRunException("A provider of a demand cannot have a later dueTime.");
            }

            Demands stockExchangeDemands = ZppConfiguration.CacheManager.GetAggregator()
                .GetAllChildDemandsOf(stockExchangeProvider);
            if (stockExchangeDemands.Any() == false)
                // StockExchangeProvider has no childs (stockExchangeDemands),
                // take that from stockExchangeProvider
            {
                DueTime childDueTime = stockExchangeProvider.GetStartTime();
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
                    DueTime stockExchangeDemandDueTime = stockExchangeDemand.GetStartTime();
                    if (stockExchangeDemandDueTime.IsGreaterThan(earliestStartTime))
                    {
                        earliestStartTime = stockExchangeDemandDueTime;
                    }
                }
            }

            return earliestStartTime;
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
                if (aggregator.GetAllChildProvidersOf(customerOrderPart).Any() == false)
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
            return new DemandOrProviders(demandOrProviders.GetAll()
                .Where(x => simulationInterval.IsWithinInterval(x.GetStartTime())));
        }

        public DemandOrProviders GetDemandsOrProvidersWhereEndTimeIsWithinIntervalOrBefore(
            SimulationInterval simulationInterval, DemandOrProviders demandOrProviders)
        {
            // endTime within interval
            return new DemandOrProviders(demandOrProviders.GetAll().Where(x =>
            {
                DueTime endTime = x.GetEndTime();
                return simulationInterval.IsWithinInterval(endTime) ||
                       simulationInterval.IsBeforeInterval(endTime);
            }));
        }

        /**
         * DemandToProvider
         */
        public IEnumerable<ILinkDemandAndProvider> GetArrowsTo(Provider provider)
        {
            return _dbTransactionData.DemandToProviderGetAll().GetAll()
                .Where(x => x.GetProviderId().Equals(provider.GetId()));
        }

        /**
         * ProviderToDemand
         */
        public IEnumerable<ILinkDemandAndProvider> GetArrowsFrom(Provider provider)
        {
            return _dbTransactionData.ProviderToDemandGetAll().GetAll()
                .Where(x => x.GetProviderId().Equals(provider.GetId()));
        }

        /**
         * ProviderToDemand
         */
        public IEnumerable<ILinkDemandAndProvider> GetArrowsTo(Demand demand)
        {
            return _dbTransactionData.ProviderToDemandGetAll().GetAll()
                .Where(x => x.GetDemandId().Equals(demand.GetId()));
        }

        /**
         * DemandToProvider
         */
        public IEnumerable<ILinkDemandAndProvider> GetArrowsFrom(Demand demand)
        {
            return _dbTransactionData.DemandToProviderGetAll().GetAll()
                .Where(x => x.GetDemandId().Equals(demand.GetId()));
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
    }
}