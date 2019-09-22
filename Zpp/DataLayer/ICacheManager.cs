using Master40.DB.Data.Context;
using Zpp.Mrp.NodeManagement;

namespace Zpp.DbCache
{
    /**
     * Controls data layer objects like dbMasterCache, dbTransactionData to force singleton pattern
     */
    public interface ICacheManager
    {
        void InitByReadingFromDatabase(ProductionDomainContext productionDomainContext);

        IDbTransactionData ReloadTransactionData();

        IDbMasterDataCache GetMasterDataCache();

        IDbTransactionData GetDbTransactionData();

        IOpenDemandManager GetOpenDemandManager();
    }
}