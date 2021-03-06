using System;
using System.IO;
using Master40.DB.Data.Context;
using Master40.DB.Data.WrappersForPrimitives;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Zpp.DataLayer.impl.OpenDemand;
using Zpp.Mrp2.impl.Mrp1.impl.LotSize.Impl;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Test.Configuration;
using Zpp.Util;
using Zpp.Util.Graph.impl;

namespace Zpp.DataLayer.impl
{
    public class CacheManager : ICacheManager
    {
        private DbTransactionData _dbTransactionData;
        private DbTransactionData _dbTransactionDataBackup;
        private DbTransactionData _dbTransactionDataArchive;
        private DbMasterDataCache _dbMasterDataCache;
        private ProductionDomainContext _productionDomainContext;
        private ProductionDomainContext _productionDomainContextArchive;
        private IOpenDemandManager _openDemandManager;
        private TestConfiguration _testConfiguration;
        private IAggregator _aggregator;

        public void InitByReadingFromDatabase(string testConfiguration, bool addInitialStockLevels)
        {
            ProductionDomainContexts productionDomainContexts = Dbms.GetDbContext();
            _productionDomainContext = productionDomainContexts.ProductionDomainContext;

            _productionDomainContextArchive =
                productionDomainContexts.ProductionDomainContextArchive;

            InitDb(testConfiguration, _productionDomainContext, true);
            InitDb(testConfiguration, _productionDomainContextArchive, false);

            _dbMasterDataCache = new DbMasterDataCache(_productionDomainContext);
            // duplicate masterData for archive
            _dbMasterDataCache.Clone(_productionDomainContextArchive);

            _dbTransactionData = new DbTransactionData(_productionDomainContext);
            _dbTransactionDataArchive = new DbTransactionData(_productionDomainContextArchive);

            if (addInitialStockLevels)
            {
                OpenDemandManager.AddInitialStockLevels(_dbTransactionData);
            }

            _aggregator = new Aggregator(_dbTransactionData);
            _openDemandManager = new OpenDemandManager();
        }

        public IDbTransactionData ReloadTransactionData()
        {
            _dbTransactionData = new DbTransactionData(_productionDomainContext);
            _dbTransactionDataArchive = new DbTransactionData(_productionDomainContextArchive);
            _aggregator = new Aggregator(_dbTransactionData);
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

        public IDbTransactionData GetDbTransactionDataArchive()
        {
            return _dbTransactionDataArchive;
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
        private void InitDb(string testConfiguration,
            ProductionDomainContext productionDomainContext, bool InitData)
        {
            if (ZppConfiguration.CacheManager.GetTestConfiguration() == null)
            {
                ReadInTestConfiguration(testConfiguration);
            }

            if (Constants.IsLocalDb)
            {
                bool isDeleted = productionDomainContext.Database.EnsureDeleted();
                if (!isDeleted)
                {
                    // pass
                }
            }

            /*
             // random name currently, this doesn't work on linux/unix anyway
             else if(Constants.IsLocalDb == false && Constants.IsWindows)
            {
                bool wasDropped = Dbms.DropDatabase(
                    Constants.GetDbName(),
                    Constants.GetConnectionString(Constants.DefaultDbName));
                if (wasDropped == false)
                {
                    // pass
                }
            }*/

            if (InitData)
            {
                Type dbSetInitializer = Type.GetType(_testConfiguration.DbSetInitializer);
                dbSetInitializer.GetMethod("DbInitialize").Invoke(null, new[]
                {
                    productionDomainContext
                });
            }
            else
            {
                productionDomainContext.Database.EnsureCreated();
            }

            LotSize.SetDefaultLotSize(new Quantity(_testConfiguration.LotSize));
            LotSize.SetLotSizeType(_testConfiguration.LotSizeType);
        }

        public void ReadInTestConfiguration(string testConfigurationFileNames)
        {
            _testConfiguration = JsonConvert.DeserializeObject<TestConfiguration>(
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
            _openDemandManager?.Dispose();
            _openDemandManager = null;
            _dbMasterDataCache = null;
            _testConfiguration = null;

            _productionDomainContext?.Database?.CloseConnection();
            _dbTransactionData?.Dispose();

            _productionDomainContextArchive?.Database?.CloseConnection();
            _dbTransactionDataArchive?.Dispose();

            _productionDomainContext = null;
            _productionDomainContextArchive = null;

            _dbTransactionData = null;
            _dbTransactionDataArchive = null;
        }

        public IAggregator GetAggregator()
        {
            return _aggregator;
        }

        public void Persist()
        {
            _dbTransactionData.PersistDbCache();
            _dbTransactionDataArchive.PersistDbCache();
        }

        public void UseArchiveForGetters()
        {
            _dbTransactionDataBackup = _dbTransactionData; 
            _dbTransactionData = _dbTransactionDataArchive;
            _aggregator = new Aggregator(_dbTransactionDataArchive);
        }

        public void UseArchiveForGettersRevert()
        {
            if (_dbTransactionDataBackup == null)
            {
                return;
            }

            _dbTransactionData = _dbTransactionDataBackup;
            _aggregator = new Aggregator(_dbTransactionData);
        }
    }
}