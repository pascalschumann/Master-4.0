using System.Collections.Generic;
using System.Linq;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.ZppSimulator.impl.Confirmation.impl
{
    public class ConfirmationManager : IConfirmationManager
    {
        public void CreateConfirmations(SimulationInterval simulationInterval)
        {
            ConfirmationCreator.CreateConfirmations(simulationInterval);
        }

        public void ApplyConfirmations()
        {
            ConfirmationAppliance.ApplyConfirmations();
        }
    }
}