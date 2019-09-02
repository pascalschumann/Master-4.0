using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DemandDomain;
using Zpp.MachineDomain;
using Zpp.ProviderDomain;
using Zpp.Utils;

namespace Zpp
{
    public class Aggregator : IAggregator
    {
        private readonly IDbMasterDataCache _dbMasterDataCache;
        private readonly IDbTransactionData _dbTransactionData;

        public Aggregator(IDbMasterDataCache dbMasterDataCache,
            IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;
            _dbMasterDataCache = dbMasterDataCache;
        }

        public ProductionOrderBoms GetProductionOrderBomsOfProductionOrder(
            ProductionOrder productionOrder)
        {
            throw new System.NotImplementedException();
        }

        public List<Machine> GetMachinesOfProductionOrderOperation(
            ProductionOrderOperation productionOrderOperation)
        {
            return _dbMasterDataCache.M_MachineGetAll().Where(x =>
                x.GetMachineGroupId().GetValue()
                    .Equals(productionOrderOperation.GetValue().MachineGroupId)).ToList();
        }

        public List<ProductionOrderOperation> GetProductionOrderOperationsOfMachine(Machine machine)
        {
            throw new System.NotImplementedException();
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

        public Demands GetDemandsOfProvider(Provider provider)
        {
            List<Demand> demands = new List<Demand>();
            foreach (var demandToProvider in _dbTransactionData.DemandToProviderGetAll().GetAll())
            {
                if (demandToProvider.GetProviderId().Equals(provider.GetId()))
                {
                    demands.Add(_dbTransactionData.DemandsGetById(demandToProvider.GetDemandId()));
                }
            }

            return new Demands(demands);
        }

        public ProductionOrderBom GetAnyProductionOrderBomByProductionOrderOperation(
            ProductionOrderOperation productionOrderOperation)
        {
            T_ProductionOrderBom productionOrderBom = _dbTransactionData.ProductionOrderBomGetAll().GetAllAs<T_ProductionOrderBom>().Find(x =>
                x.ProductionOrderOperationId.Equals(productionOrderOperation.GetId().GetValue()));
            if (productionOrderBom == null)
            {
                throw new MrpRunException("How could an productionOrderOperation without an T_ProductionOrderBom exists?");
            }
                
            return new ProductionOrderBom(productionOrderBom,_dbMasterDataCache);
        }

        public ProductionOrderBoms GetAllProductionOrderBomsBy(ProductionOrderOperation productionOrderOperation)
        {
            List<T_ProductionOrderBom> productionOrderBoms = _dbTransactionData.ProductionOrderBomGetAll().GetAllAs<T_ProductionOrderBom>().FindAll(x =>
                x.ProductionOrderOperationId.Equals(productionOrderOperation.GetId().GetValue()));
            if (productionOrderBoms == null || productionOrderBoms.Any() == false)
            {
                throw new MrpRunException("How could an productionOrderOperation without an T_ProductionOrderBom exists?");
            }
                
            return new ProductionOrderBoms(productionOrderBoms, _dbMasterDataCache);
        }

        public Providers GetAllProviderOfDemand(Demand demand, IDbTransactionData dbTransactionData)
        {
            Providers providers = new Providers();
            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll())
            {
                if (demandToProvider.GetDemandId().Equals(demand.GetId()))
                {
                    providers.Add(dbTransactionData.ProvidersGetById(demandToProvider.GetProviderId()));
                }
                
            }
            return providers;
        }
    }
}