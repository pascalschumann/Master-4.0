using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.Mrp2.impl.Mrp1.impl.Production.impl.ProductionTypes;

namespace Zpp
{
    public static class ZppConfiguration
    {
        // public static ProductionType ProductionType = ProductionType.AssemblyLine;
        // public static ProductionType ProductionType = ProductionType.WorkshopProduction;
        public static ProductionType ProductionType = ProductionType.WorkshopProductionClassic;
        
        public static ICacheManager CacheManager = new CacheManager();
    }
}