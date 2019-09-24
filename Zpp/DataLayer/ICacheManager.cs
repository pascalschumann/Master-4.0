using Master40.DB.Data.Context;
using Zpp.Mrp.NodeManagement;
using Zpp.Test.Configuration;

namespace Zpp.DbCache
{
    /**
     * Controls data layer objects like dbMasterCache, dbTransactionData to force singleton pattern
     */
    public interface ICacheManager
    {
        void InitByReadingFromDatabase(string testConfiguration);
        
        IDbTransactionData ReloadTransactionData();

        IDbMasterDataCache GetMasterDataCache();
        
        /**
         * Don't store the returned reference, since it becomes invalid on ReloadTransactionData() call
         */
        IDbTransactionData GetDbTransactionData();

        /**
         * Don't store the returned reference, since it becomes invalid on ReloadTransactionData() call
         */
        IOpenDemandManager GetOpenDemandManager();

        ProductionDomainContext GetProductionDomainContext();

        TestConfiguration GetTestConfiguration();

        /**
         * Free resources like dbConnections
         */
        void Dispose();

        IAggregator GetAggregator();
    }
}