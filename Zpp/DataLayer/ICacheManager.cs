using Master40.DB.Data.Context;

namespace Zpp.DbCache
{
    public interface ICacheManager
    {
        void InitByReadingFromDatabase(ProductionDomainContext productionDomainContext);

        IDbTransactionData ReloadTransactionData();

        IDbMasterDataCache GetMasterDataCache();

        IDbTransactionData GetDbTransactionData();
    }
}