using Master40.DB.Data.Context;

namespace Zpp.DbCache
{
    public class CacheManager: ICacheManager
    {
        private IDbTransactionData _dbTransactionData;
        private IDbMasterDataCache _dbMasterDataCache;
        private ProductionDomainContext _productionDomainContext;
        
        public void InitByReadingFromDatabase(ProductionDomainContext productionDomainContext)
        {
            _dbMasterDataCache = new DbMasterDataCache(productionDomainContext);
            _dbTransactionData = new DbTransactionData(productionDomainContext);
            _productionDomainContext = productionDomainContext;
        }

        public IDbTransactionData ReloadTransactionData()
        {
            _dbTransactionData = new DbTransactionData(_productionDomainContext);
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
    }
}