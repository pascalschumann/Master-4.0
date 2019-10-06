using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer.DemandDomain;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.ProviderDomain;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.DataLayer.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.WrappersForCollections;
using Zpp.Scheduling.impl.JobShop.impl;
using Zpp.Simulation.impl.Types;

namespace Zpp.DataLayer
{
    /**
     * A layer over masterData/transactionData that provides aggregations of entities from masterData/transactionData
     */
    public interface IAggregator
    {
        ProductionOrderBoms
            GetProductionOrderBomsOfProductionOrder(ProductionOrder productionOrder);

        List<Resource> GetResourcesByResourceSkillId(Id resourceSkillId);

        List<ProductionOrderOperation> GetProductionOrderOperationsOfProductionOrder(
            ProductionOrder productionOrder);

        List<ProductionOrderOperation> GetProductionOrderOperationsOfProductionOrder(
            Id productionOrderId);

        ProductionOrderBom GetAnyProductionOrderBomByProductionOrderOperation(
            ProductionOrderOperation productionOrderOperation);

        ProductionOrderBoms GetAllProductionOrderBomsBy(
            ProductionOrderOperation productionOrderOperation);

        Providers GetAllChildProvidersOf(Demand demand);

        Providers GetAllParentProvidersOf(Demand demand);

        Demands GetAllParentDemandsOf(Provider provider);

        Demands GetAllChildDemandsOf(Provider provider);


        List<Provider> GetProvidersForInterval(DueTime from, DueTime to);

        DueTime GetEarliestPossibleStartTimeOf(ProductionOrderBom productionOrderBom);

        Demands GetPendingCustomerOrderParts();

        DemandOrProviders GetDemandsOrProvidersWhereEndTimeIsWithinInterval(
            SimulationInterval simulationInterval, DemandOrProviders demandOrProviders);

        DemandOrProviders GetDemandsOrProvidersWhereStartTimeIsWithinInterval(
            SimulationInterval simulationInterval, DemandOrProviders demandOrProviders);

    }
}