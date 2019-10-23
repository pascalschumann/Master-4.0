using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Interfaces;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Mrp2.impl.Scheduling.impl.JobShopScheduler;
using Zpp.ZppSimulator.impl;

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

        /**
         * Traverse down till including StockExchangeDemands and calculate max endTime of the children
         */
        DueTime GetEarliestPossibleStartTimeOf(ProductionOrderBom productionOrderBom);

        Demands GetPendingCustomerOrderParts();

        DemandOrProviders GetDemandsOrProvidersWhereEndTimeIsWithinInterval(
            SimulationInterval simulationInterval, DemandOrProviders demandOrProviders);

        DemandOrProviders GetDemandsOrProvidersWhereStartTimeIsWithinInterval(
            SimulationInterval simulationInterval, DemandOrProviders demandOrProviders);

        /**
         * Arrow equals DemandToProvider and ProviderToDemand
         */
        List<ILinkDemandAndProvider> GetArrowsToAndFrom(Provider provider);

        /**
         * Arrow equals DemandToProvider and ProviderToDemand
         */
        List<ILinkDemandAndProvider> GetArrowsToAndFrom(Demand demand);
        
        /**
         * Arrow equals DemandToProvider and ProviderToDemand
         */
        List<ILinkDemandAndProvider> GetArrowsToAndFrom(IDemandOrProvider demandOrProvider);
    }
}