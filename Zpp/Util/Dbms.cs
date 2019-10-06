using System;
using System.Data;
using System.Data.SqlClient;
using Master40.DB.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Zpp.Util
{
    public static class Dbms
    {
        private static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();

        public static readonly LoggerFactory MyLoggerFactory = new LoggerFactory();


    public static ProductionDomainContext GetDbContext()
        {
            ProductionDomainContext productionDomainContext;

            // EF inMemory
            // MasterDBContext _inMemmoryContext = new MasterDBContext(new DbContextOptionsBuilder<MasterDBContext>()
            /*_productionDomainContext = new ProductionDomainContext(new DbContextOptionsBuilder<MasterDBContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDB")
                .Options);*/

            if (Constants.UseLocalDb() && Constants.IsWindows)
            {
                productionDomainContext = new ProductionDomainContext(
                    new DbContextOptionsBuilder<MasterDBContext>().UseLoggerFactory(MyLoggerFactory)
                        .UseSqlServer(
                            // Constants.DbConnectionZppLocalDb)
                            Constants.GetConnectionString()).Options);
                    Constants.IsLocalDb = true;
            } else if (Constants.IsWindows)
            {
                // Windows
                productionDomainContext = new ProductionDomainContext(
                    new DbContextOptionsBuilder<MasterDBContext>().UseLoggerFactory(MyLoggerFactory)
                        .UseSqlServer(
                            // Constants.DbConnectionZppLocalDb)
                            Constants.GetConnectionString()).Options);
                Constants.IsLocalDb = false;
            }
            else
            {
                // With Sql Server for Mac/Linux
                productionDomainContext = new ProductionDomainContext(
                    new DbContextOptionsBuilder<MasterDBContext>().UseLoggerFactory(MyLoggerFactory)
                        .UseSqlServer(Constants.GetConnectionString()).Options);
            }

            MyLoggerFactory.AddNLog();

            // disable tracking (https://docs.microsoft.com/en-us/ef/core/querying/tracking)
            productionDomainContext.ChangeTracker.QueryTrackingBehavior =
                QueryTrackingBehavior.NoTracking;

            return productionDomainContext;
        }

        public static bool CanConnect(string connectionString)
        {
            bool canConnect = false;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();
                    canConnect = con.State == ConnectionState.Open;
                }
                catch (SqlException e)
                {
                    canConnect = false;
                }
            }
            return canConnect;
        }

        /**
         * @return: true, if db was succesfully dropped
         */
        public static bool DropDatabase(string dbName, string connectionString)
        {
            if (CanConnect(Constants.GetConnectionString()) == false)
            {
                return false;
            }
            int result = 0;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                String sqlCommandText = @"
                ALTER DATABASE " + dbName + @" SET OFFLINE WITH ROLLBACK IMMEDIATE;
                ALTER DATABASE " + dbName + @" SET ONLINE;
                DROP DATABASE [" + dbName + "]";

                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, con);
                try
                {
                    result = sqlCommand.ExecuteNonQuery();
                }
                catch (SqlException sqlException)
                {
                    return false;
                }
            }

            // For UPDATE, INSERT, and DELETE statements, the return value is the number of rows
            // affected by the command. For all other types of statements, the return value is -1
            // source: https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.executenonquery?view=netframework-4.8
            return result == -1;
        }
    }
}