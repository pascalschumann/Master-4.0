using System.Collections.Generic;
using System.Linq;
using Zpp.DemandDomain;
using Zpp.MachineDomain;
using Zpp.ProviderDomain;

namespace Zpp
{
    public class Aggregator : IAggregator
    {
        private readonly IDbMasterDataCache _dbMasterDataCache;
        private readonly IDbTransactionData _dbTransactionData;
        
        public Aggregator(IDbMasterDataCache dbMasterDataCache, IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;
            _dbMasterDataCache = dbMasterDataCache;
        }

        public ProductionOrderBoms GetProductionOrderBomsOfProductionOrder(ProductionOrder productionOrder)
        {
            throw new System.NotImplementedException();
        }

        public List<Machine> GetMachinesOfProductionOrderOperation(ProductionOrderOperation productionOrderOperation)
        {
            return _dbMasterDataCache.M_MachineGetAll().Where(x =>
                x.GetMachineGroupId().GetValue().Equals(productionOrderOperation.GetValue().MachineGroupId)).ToList();
        }

        public List<ProductionOrderOperation> GetProductionOrderOperationsOfMachine(Machine machine)
        {
            throw new System.NotImplementedException();
        }

        public List<ProductionOrderOperation> GetProductionOrderOperationsOfProductionOrder(ProductionOrder productionOrder)
        {
            throw new System.NotImplementedException();
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
    }
}