using System;
using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Newtonsoft.Json;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Configuration;
using Zpp.DbCache;

namespace Zpp.GraphicalRepresentation
{
    public class GanttChart : IGanttChart
    {
        private readonly IDbMasterDataCache _dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();
        private readonly List<GanttChartBar> _ganttChartBars = new List<GanttChartBar>();

        public GanttChart(IEnumerable<ProductionOrderOperation> productionOrderOperations)
        {
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                GanttChartBar ganttChartBar = new GanttChartBar();
                T_ProductionOrderOperation tProductionOrderOperation = productionOrderOperation.GetValue();

                ganttChartBar.operation = productionOrderOperation.ToString();
                ganttChartBar.operationId = tProductionOrderOperation.Id.ToString();
                if (tProductionOrderOperation.Resource == null)
                {
                    tProductionOrderOperation.Resource = _dbMasterDataCache
                        .M_ResourceGetById(new Id(tProductionOrderOperation.ResourceId
                            .GetValueOrDefault())).GetValue();
                }

                ganttChartBar.resource = tProductionOrderOperation.Resource.ToString();
                ganttChartBar.start = tProductionOrderOperation.Start.ToString();
                ganttChartBar.end = tProductionOrderOperation.End.ToString();

                ganttChartBar.groupId = productionOrderOperation.GetProductionOrderId().ToString();

                AddGanttChartBar(ganttChartBar);

            }
        }

        public void AddGanttChartBar(GanttChartBar ganttChartBar)
        {
            _ganttChartBars.Add(ganttChartBar);
        }

        public List<GanttChartBar> GetAllGanttChartBars()
        {
            return _ganttChartBars;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(_ganttChartBars, Formatting.Indented);
        }
    }
}