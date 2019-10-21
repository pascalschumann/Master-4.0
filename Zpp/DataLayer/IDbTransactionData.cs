using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer.impl.DemandDomain;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="demandToProvidersMap">is used to generate T_Demand and T_Provider tables</param>
        void PersistDbCache();

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

        ProductionOrders ProductionOrderGetAll();
        
        ProductionOrder ProductionOrderGetById(Id id);

        IDemands DemandsGetAll();

        IProviderToDemandTable ProviderToDemandGetAll();
        
        IDemandToProviderTable DemandToProviderGetAll();

        Demand DemandsGetById(Id id);
        
        Provider ProvidersGetById(Id id);

        ProductionOrderOperation ProductionOrderOperationGetById(Id id);
        
        ProductionOrderOperations ProductionOrderOperationGetAll();

        T_CustomerOrder T_CustomerOrderGetById(Id id);
        
        List<T_CustomerOrder> T_CustomerOrderGetAll();

        Demands T_CustomerOrderPartGetAll();

        void CustomerOrderPartAdd(T_CustomerOrderPart customerOrderPart);
        
        void CustomerOrderAdd(T_CustomerOrder customerOrder);

        void DemandToProviderAdd(T_DemandToProvider demandToProvider);

        void ProviderToDemandAddAll(ProviderToDemandTable providerToDemandTable);
        
        void Dispose();

        void AddAll(EntityCollector entityCollector);

        void ProductionOrderOperationAdd(T_ProductionOrderOperation productionOrderOperation);

        void DeleteStockExchangeProvider(StockExchangeProvider stockExchangeProvider);

        void DeleteDemandToProvider(T_DemandToProvider demandToProvider);
        
        void DeleteProviderToDemand(T_ProviderToDemand providerToDemand);
        
        void DeleteAllDemandToProvider(IEnumerable<T_DemandToProvider> demandToProviders);
        
        void DeleteAllProviderToDemand(IEnumerable<T_ProviderToDemand> providerToDemands);
    }
}