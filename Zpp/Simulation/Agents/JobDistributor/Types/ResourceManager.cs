using System;
using System.Collections.Generic;
using System.Text;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;

namespace Zpp.Simulation.Agents.JobDistributor.Types
{
    public class ResourceManager 
    {
        private Dictionary<Id, ResourceDetails> Resources = new Dictionary<Id, ResourceDetails>();

        public int Count => Resources.Count;

        internal void AddResource(ResourceDetails resource)
        {
            Resources.TryAdd(resource.Machine.GetId(), resource);
        }

        /// <summary>
        /// Get all available Resources as Dictionary<Id, Machine>
        /// </summary>
        /// <returns>ResourceDictionary</returns>
        public static ResourceDictionary GetResources(IDbMasterDataCache masterDataCache)
        {
            var resources = new ResourceDictionary();
            foreach (var machineGroup in masterDataCache.M_MachineGroupGetAll())
            {
                resources.Add(machineGroup.GetId(),
                    masterDataCache.M_MachineGetAllByMachineGroupId(machineGroup.GetId()));
            }
            return resources;
        }
    }
}
