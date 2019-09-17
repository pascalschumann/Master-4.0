using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Master40.DB.Data.Helper;

namespace Zpp.Utils
{
    public static class Constants
    {
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsLocalDb = false;
        // TODO: the random/dateTime is a workaround, remove this if drop database query in Dispose() in TestClasses is added
        private static readonly string DateString = GetDateString();

        public static string GetDbName()
        {
            if (IsWindows)
            {
                // use always the same databaseName and drop db before the next test
                return "zpp2";
            }
            else
            {
                // never got this feature (reuse same dbName) working
                return $"zpp{DateString}";
            }
        }

        private static string GetDateString()
        {
            string ticks = DateTime.UtcNow.Ticks.ToString();
            // the last 9 decimal places of ticks are enough
            return DateTime.Now.ToString("MM-dd_HH:mm") + $"__{ticks.Substring(10, ticks.Length-10)}";
        }
        
        private static String DbConnectionZppLocalDb { get; } =
            $"Server=(localdb)\\mssqllocaldb;Database=UnitTestDB;Trusted_Connection=True;MultipleActiveResultSets=true";

        private static String DbConnectionZppSqlServer()
        {
            return $"Server=localhost,1433;Database={GetDbName()};" +
                   $"MultipleActiveResultSets=true;User ID=SA;Password=123*Start#";
        }

        public static string GetConnectionString()
        {
            if (UseLocalDb() && Constants.IsWindows)
            {
                return Constants.DbConnectionZppLocalDb;
            }
            else
            {
                return DbConnectionZppSqlServer();
            }
        }

        public static string EnumToString<T>(T enumValue, Type enumType)
        {
            return Enum.GetName(enumType, enumValue);
        }
        
        /// <summary>
        /// If localDb shall be used - set it via command line with :
        /// setx UseLocalDb true
        /// </summary>
        /// <returns></returns>
        public static bool UseLocalDb()
        {
            var environmentUseLocalDb = Environment.GetEnvironmentVariable("UseLocalDb", EnvironmentVariableTarget.User);
            if (environmentUseLocalDb != null)
                return environmentUseLocalDb.Equals("true");
            return false;
        }        
    }
}