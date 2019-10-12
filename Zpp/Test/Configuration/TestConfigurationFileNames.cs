namespace Zpp.Test.Configuration
{
    public static class TestConfigurationFileNames
    {
        private const string _basePath = "../../../Test/Configuration/";

        // TODO: maybe all but one for truck and one for desk can be removed,
        // since order are generated independent of these config files´(lotsize is still considered)

        // desk
        public const string DESK_COP_5_LOTSIZE_2 =
            _basePath + "tisch_cop_5_lotsize_2.json";
        
        public const string DESK_COP_2_LOTSIZE_2 =
            _basePath + "tisch_cop_2_lotsize_2.json";

        // truck
        public const string TRUCK_COP_5_LOTSIZE_2 = _basePath + "truck_cop_5_lotsize_2.json";
        public const string TRUCK_COP_1_LOTSIZE_1 = _basePath + "truck_cop_1_lotsize_1.json";
    }
}