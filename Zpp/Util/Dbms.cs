using System;
using System.Data;
using System.Data.SqlClient;
using Master40.DB.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Zpp.Util
{
    public static class Dbms
    {
        
        // In case you need detail ef log, reAdd Nlog and enable following statement
        // public static readonly LoggerFactory MyLoggerFactory = new LoggerFactory();

        
        
    public static ProductionDomainContexts GetDbContext()
        {
            ProductionDomainContexts productionDomainContexts = new ProductionDomainContexts();
            
            // In case you need detail ef log, reAdd Nlog and enable commented out part
            ProductionDomainContext productionDomainContext = new ProductionDomainContext(
                new DbContextOptionsBuilder<MasterDBContext>()// .UseLoggerFactory(MyLoggerFactory)
                    .UseSqlServer(Constants.GetConnectionString(Constants.DefaultDbName)).Options);
            productionDomainContexts.ProductionDomainContext = productionDomainContext;
            
            ProductionDomainContext productionDomainContextArchive = new ProductionDomainContext(
                new DbContextOptionsBuilder<MasterDBContext>()// .UseLoggerFactory(MyLoggerFactory)
                    .UseSqlServer(Constants.GetConnectionString(Constants.DefaultDbName + "_archive")).Options);
            productionDomainContexts.ProductionDomainContextArchive =
                productionDomainContextArchive;
            
            if (Constants.UseLocalDb() && Constants.IsWindows)
            {
                Constants.IsLocalDb = true;
            } else if (Constants.UseLocalDb() == false && Constants.IsWindows)
            {
                
                Constants.IsLocalDb = false;
            }
            else
            {
                // With Sql Server for Mac/Linux

            }
            // In case you need detail ef log, reAdd Nlog and enable following statement
            // MyLoggerFactory.AddNLog();

            // disable tracking (https://docs.microsoft.com/en-us/ef/core/querying/tracking)
            productionDomainContext.ChangeTracker.QueryTrackingBehavior =
                QueryTrackingBehavior.NoTracking;
            productionDomainContextArchive.ChangeTracker.QueryTrackingBehavior =
                QueryTrackingBehavior.NoTracking;

            return productionDomainContexts;
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
            if (CanConnect(Constants.GetConnectionString(Constants.DefaultDbName)) == false)
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