using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.Mrp.MachineManagement;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.DbCache
{
    public class Aggregator : IAggregator
    {
        private readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();

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
                    "How could an productionOrderOperation without an T_ProductionOrderBom exists?");
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
                x.GetDueTime().GetValue() >= from.GetValue() &&
                x.GetDueTime().GetValue() <= to.GetValue());
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
    }
}