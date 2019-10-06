using EntityFrameworkCore.Cacheable;
using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.Production.impl.ProductionTypes;

namespace Zpp.Configuration
{
    public static class ZppConfiguration
    {
        // public static ProductionType ProductionType = ProductionType.AssemblyLine;
        // public static ProductionType ProductionType = ProductionType.WorkshopProduction;
        public static ProductionType ProductionType = ProductionType.WorkshopProductionClassic;
        
        public static ICacheManager CacheManager = new CacheManager();
    }
}