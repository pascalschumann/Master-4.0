using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.DataLayer.impl.WrappersForCollections;

namespace Zpp.DataLayer
{
    
    /**
     * Do NOT store a reference to this class, store ICacheManager instead and call GetDbTransactionData()
     */
    public interface IDbTransactionData
    {
        // TODO: M_* methods should be removed
        M_Article M_ArticleGetById(Id id);
        
        M_ArticleBom M_ArticleBomGetById(Id id);
        
        List<M_BusinessPartner> M_BusinessPartnerGetAll();
        
        void DemandToProvidersRemoveAll();

        void DemandsAdd(Demand demand);
        
        void DemandsAddAll(IDemands demands);

        IProviders ProvidersGetAll();

        void ProvidersAdd(Provider provider);
        
        void ProvidersAddAll(IProviders providers);
        
        ProductionOrderBoms ProductionOrderBomGetAll();
        
        StockExchangeProviders StockExchangeProvidersGetAll();
        
        StockExchangeDemands StockExchangeDemandsGetAll();
        
        PurchaseOrderParts PurchaseOrderPartGetAll();
        
        T_PurchaseOrder PurchaseOrderGetById(Id id);
        
        List<T_PurchaseOrder> PurchaseOrderGetAll();
        
        void PurchaseOrderDelete(T_PurchaseOrder purchaseOrder);
        
        void PurchaseOrderAdd(T_PurchaseOrder purchaseOrder);

        ProductionOrders ProductionOrderGetAll();
        
        ProductionOrder ProductionOrderGetById(Id id);

        IDemands DemandsGetAll();

        IProviderToDemandTable ProviderToDemandGetAll();
        
        IDemandToProviderTable DemandToProviderGetAll();

        Demand DemandsGetById(Id id);
        
        Provider ProvidersGetById(Id id);

        ProductionOrderOperation ProductionOrderOperationGetById(Id id);
        
        ProductionOrderOperations ProductionOrderOperationGetAll();

        T_CustomerOrder CustomerOrderGetById(Id id);
        
        void T_CustomerOrderDelete(T_CustomerOrder customerOrder);
        
        List<T_CustomerOrder> CustomerOrderGetAll();

        Demands CustomerOrderPartGetAll();

        void CustomerOrderPartAdd(T_CustomerOrderPart customerOrderPart);
        
        void CustomerOrderAdd(T_CustomerOrder customerOrder);

        void DemandToProviderAdd(T_DemandToProvider demandToProvider);

        void ProviderToDemandAddAll(ProviderToDemandTable providerToDemandTable);
        
        void ProviderToDemandAdd(T_ProviderToDemand providerToDemand);
        
        void Dispose();

        void AddAllFrom(EntityCollector entityCollector);

        void ProductionOrderOperationAdd(ProductionOrderOperation productionOrderOperation);
        
        void ProductionOrderOperationAddAll(List<ProductionOrderOperation> productionOrderOperations);

        void StockExchangeProvidersDelete(StockExchangeProvider stockExchangeProvider);

        void DeleteA(IDemandOrProvider demandOrProvider);
        
        void DeleteA(ILinkDemandAndProvider demandAndProviderLink);
        
        void DeleteAllFrom(List<IDemandOrProvider> demandOrProviders);
        
        void DeleteAllFrom(List<ILinkDemandAndProvider> demandAndProviderLinks);
        
        void DemandsDelete(Demand demand);

        void ProvidersDelete(Provider provider);

        void DemandToProviderDelete(T_DemandToProvider demandToProvider);
        
        void ProviderToDemandDelete(T_ProviderToDemand providerToDemand);
        
        void DemandToProviderDeleteAll(IEnumerable<T_DemandToProvider> demandToProviders);
        
        void ProviderToDemandDeleteAll(IEnumerable<T_ProviderToDemand> providerToDemands);

        void ProductionOrderOperationDeleteAll(List<ProductionOrderOperation> productionOrderOperations);
        
        void ProductionOrderOperationDelete(ProductionOrderOperation productionOrderOperation);

        void AddAllFrom(List<IDemandOrProvider> demandOrProviders);
        
        void AddA(IDemandOrProvider demandOrProvider);
        
        void AddAllFrom(List<ILinkDemandAndProvider> demandOrProviders);
        
        void AddA(ILinkDemandAndProvider demandAndProviderLink);
        
        }
}