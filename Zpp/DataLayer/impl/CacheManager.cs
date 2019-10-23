using System;
using System.IO;
using Master40.DB.Data.Context;
using Master40.DB.Data.WrappersForPrimitives;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Zpp.DataLayer.impl.OpenDemand;
using Zpp.Mrp2.impl.Mrp1.impl.LotSize.Impl;
using Zpp.Test.Configuration;
using Zpp.Util;

namespace Zpp.DataLayer.impl
{
    public class CacheManager: ICacheManager
    {
        private IDbTransactionData _dbTransactionData;
        private IDbMasterDataCache _dbMasterDataCache;
        private ProductionDomainContext _productionDomainContext;
        private IOpenDemandManager _openDemandManager;
        private TestConfiguration _testConfiguration;
        private IAggregator _aggregator;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        
        public void InitByReadingFromDatabase(string testConfiguration)
        {
            _productionDomainContext = Dbms.GetDbContext();
            InitDb(testConfiguration);
            _dbMasterDataCache = new DbMasterDataCache(_productionDomainContext);
            _dbTransactionData = new DbTransactionData(_productionDomainContext);
            _aggregator = new Aggregator(_dbTransactionData);
            _openDemandManager = new OpenDemandManager(true);
        }

        public IDbTransactionData ReloadTransactionData()
        {
            _dbTransactionData = new DbTransactionData(_productionDomainContext);
            _openDemandManager = new OpenDemandManager(false);
            _aggregator = new Aggregator(_dbTransactionData);
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
            return _openDemandManager;
        }
        
        /**
         * Initialize the db:
         * - deletes current
         * - creates db according to given configuration
         */
        private void InitDb(string testConfiguration)
        {
            _testConfiguration = ReadTestConfiguration(testConfiguration);
            if (Constants.IsLocalDb)
            {
                bool isDeleted = _productionDomainContext.Database.EnsureDeleted();
                if (!isDeleted)
                {
                    _logger.Error("Database could not be deleted.");
                }
            }

            else if(Constants.IsLocalDb == false && Constants.IsWindows)
            {
                bool wasDropped = Dbms.DropDatabase(
                    Constants.GetDbName(),
                    Constants.GetConnectionString());
                if (wasDropped == false)
                {
                    _logger.Warn($"Database {Constants.GetDbName()} could not be dropped.");
                }
            }

            Type dbSetInitializer = Type.GetType(_testConfiguration.DbSetInitializer);
            dbSetInitializer.GetMethod("DbInitialize").Invoke(null, new[]
            {
                _productionDomainContext
            });

            LotSize.SetDefaultLotSize(new Quantity(_testConfiguration.LotSize));
            LotSize.SetLotSizeType(_testConfiguration.LotSizeType);
        }
        
        private static TestConfiguration ReadTestConfiguration(string testConfigurationFileNames)
        {
            return JsonConvert.DeserializeObject<TestConfiguration>(
                File.ReadAllText(testConfigurationFileNames));
        }

        public ProductionDomainContext GetProductionDomainContext()
        {
            return _productionDomainContext;
        }

        public TestConfiguration GetTestConfiguration()
        {
            return _testConfiguration;
        }

        public void Dispose()
        {
            _productionDomainContext.Database.CloseConnection();
            _dbTransactionData.Dispose();
            _openDemandManager.Dispose();
            _dbTransactionData = null;
            _openDemandManager = null;
            _dbMasterDataCache = null;
            _testConfiguration = null;
            _productionDomainContext = null;
        }
        
        public IAggregator GetAggregator()
        {
            return _aggregator;
        }
    }
}