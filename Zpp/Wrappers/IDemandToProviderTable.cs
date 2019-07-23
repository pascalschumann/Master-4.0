using System.Collections.Generic;
using Master40.DB.DataModel;
using Zpp.DemandToProviderDomain;
using Zpp.ProviderDomain;

namespace Zpp
{
    public interface IDemandToProviderTable
    {
        void AddAll(IDemandToProviderTable demandToProviderTable);

        List<T_DemandToProvider> GetAll();

        int Count();

        void Add(T_DemandToProvider demandToProvider);


    }
}