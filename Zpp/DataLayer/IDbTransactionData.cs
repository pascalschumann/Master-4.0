using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Mrp;
using Zpp.Mrp.NodeManagement;
using Zpp.WrappersForCollections;

namespace Zpp.DbCache
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

        void DemandToProviderAdd(T_DemandToProvider demandToProvider);

        void ProviderToDemandAddAll(ProviderToDemandTable providerToDemandTable);
        
        void Dispose();

        void AddAll(EntityCollector entityCollector);
    }
}