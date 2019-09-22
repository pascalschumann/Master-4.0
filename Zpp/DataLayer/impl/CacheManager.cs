using Master40.DB.Data.Context;
using Zpp.Mrp.NodeManagement;

namespace Zpp.DbCache
{
    public class CacheManager: ICacheManager
    {
        private IDbTransactionData _dbTransactionData;
        private IDbMasterDataCache _dbMasterDataCache;
        private ProductionDomainContext _productionDomainContext;
        private IOpenDemandManager _openDemandManager;
        
        public void InitByReadingFromDatabase(ProductionDomainContext productionDomainContext)
        {
            _dbMasterDataCache = new DbMasterDataCache(productionDomainContext);
            _dbTransactionData = new DbTransactionData(productionDomainContext);
            _openDemandManager = new OpenDemandManager();
            _productionDomainContext = productionDomainContext;
        }

        public IDbTransactionData ReloadTransactionData()
        {
            _dbTransactionData = new DbTransactionData(_productionDomainContext);
            _openDemandManager = new OpenDemandManager();
            return _dbTransactionData;
        }

        public IDbMasterDataCache GetMasterDataCache()
        {
            return _dbMasterDataCache;
        }

        public IDbTransactionData GetDbTransactionData()
        {
            return _dbTransactionData;
        }

        public IOpenDemandManager GetOpenDemandManager()
        {
            throw new System.NotImplementedException();
        }
    }
}