using System;
using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DemandDomain;
using Zpp.DemandToProviderDomain;
using Zpp.ProviderDomain;
using Zpp.Utils;

namespace Zpp
{
    /**
     * wraps T_DemandToProvider
     */
    public class DemandToProviderTable : IDemandToProviderTable
    {
        private readonly List<T_DemandToProvider> _demandToProviderEntities =
            new List<T_DemandToProvider>();

        public DemandToProviderTable(List<T_DemandToProvider> demandToProviderEntities)
        {
            _demandToProviderEntities = demandToProviderEntities;
        }

        public DemandToProviderTable()
        {
        }

        public List<T_DemandToProvider> GetAll()
        {
            return _demandToProviderEntities;
        }

        public void AddAll(IDemandToProviderTable demandToProviderTable)
        {
            _demandToProviderEntities.AddRange(demandToProviderTable.GetAll());
        }

        public int Count()
        {
            return _demandToProviderEntities.Count;
        }
        
    }
}