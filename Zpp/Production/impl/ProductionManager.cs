using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp.NodeManagement;
using Zpp.Mrp.ProductionManagement.ProductionTypes;
using Zpp.Mrp.Scheduling;
using Zpp.Production;
using Zpp.Utils;

namespace Zpp.Mrp.ProductionManagement
{
    public class ProductionManager : ProviderManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
        private readonly ICacheManager _cacheManager =
            ZppConfiguration.CacheManager;

        public ProductionManager()
        {
        }

        public EntityCollector Satisfy(Demand demand, Quantity demandedQuantity)
        {
            if (demand.GetArticle().ToBuild == false)
            {
                throw new MrpRunException("Must be a build article.");
            }

            EntityCollector entityCollector = CreateProductionOrder(demand, demandedQuantity);

            Logger.Debug("ProductionOrder(s) created.");

            foreach (var provider in entityCollector.GetProviders())
            {
                T_DemandToProvider demandToProvider = new T_DemandToProvider()
                {
                    DemandId = demand.GetId().GetValue(),
                    ProviderId = provider.GetId().GetValue(),
                    Quantity = provider.GetQuantity().GetValue()
                };
                entityCollector.Add(demandToProvider);
            }


            return entityCollector;
        }

        private EntityCollector CreateProductionOrder(Demand demand,
            
            Quantity lotSize)
        {
            if (!demand.GetArticle().ToBuild)
            {
                throw new MrpRunException(
                    "You are trying to create a productionOrder for a purchaseArticle.");
            }

            IProductionOrderCreator productionOrderCreator;
            switch (Configuration.ZppConfiguration.ProductionType)
            {
                case ProductionType.AssemblyLine:
                    productionOrderCreator = new ProductionOrderCreatorAssemblyLine();
                    break;
                case ProductionType.WorkshopProduction:
                    productionOrderCreator = new ProductionOrderCreatorWorkshop();
                    break;
                case ProductionType.WorkshopProductionClassic:
                    productionOrderCreator = new ProductionOrderCreatorWorkshopClassic();
                    break;
                default:
                    productionOrderCreator = null;
                    break;
            }



            return productionOrderCreator.CreateProductionOrder(demand, lotSize);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="article"></param>
        /// <param name="dbTransactionData"></param>
        /// <param name="dbMasterDataCache"></param>
        /// <param name="parentProductionOrder"></param>
        /// <param name="quantity">of production article to produce
        /// --> is used for childs as: articleBom.Quantity * quantity</param>
        /// <returns></returns>
        public static Demands CreateProductionOrderBoms(M_Article article,
            Provider parentProductionOrder, Quantity quantity)
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            
            M_Article readArticle = dbTransactionData.M_ArticleGetById(article.GetId());
            if (readArticle.ArticleBoms != null && readArticle.ArticleBoms.Any())
            {
                List<Demand> newDemands = new List<Demand>();
                IProductionOrderBomCreator productionOrderBomCreator;
                switch (Configuration.ZppConfiguration.ProductionType)
                {
                    case ProductionType.AssemblyLine:
                        productionOrderBomCreator = new ProductionOrderBomCreatorAssemblyLine();
                        break;
                    case ProductionType.WorkshopProduction:
                        productionOrderBomCreator = new ProductionOrderBomCreatorWorkshop();
                        break;
                    case ProductionType.WorkshopProductionClassic:
                        productionOrderBomCreator = new ProductionOrderBomCreatorWorkshopClassic();
                        break;
                    default:
                        productionOrderBomCreator = null;
                        break;
                }

                foreach (M_ArticleBom articleBom in readArticle.ArticleBoms)
                {
                    newDemands.AddRange(
                        productionOrderBomCreator.CreateProductionOrderBomsForArticleBom(
                            articleBom, quantity,
                            (ProductionOrder)parentProductionOrder));
                }

                // backwards scheduling
                OperationBackwardsSchedule lastOperationBackwardsSchedule = null;

                IEnumerable<ProductionOrderOperation> sortedProductionOrderOperations = newDemands
                    .Select(x =>
                        ((ProductionOrderBom)x).GetProductionOrderOperation())
                    .OrderByDescending(x => x.GetValue().HierarchyNumber);

                foreach (var productionOrderOperation in sortedProductionOrderOperations)
                {
                    lastOperationBackwardsSchedule = productionOrderOperation.ScheduleBackwards(
                        lastOperationBackwardsSchedule,
                        parentProductionOrder.GetDueTime());
                }


                return new ProductionOrderBoms(newDemands);
            }

            return null;
        }

        public static T_ProductionOrderOperation CreateProductionOrderOperation(
            M_ArticleBom articleBom, Provider parentProductionOrder, Quantity quantity)
        {
            T_ProductionOrderOperation productionOrderOperation = new T_ProductionOrderOperation();
            productionOrderOperation = new T_ProductionOrderOperation();
            productionOrderOperation.Name = articleBom.Operation.Name;
            productionOrderOperation.HierarchyNumber = articleBom.Operation.HierarchyNumber;
            productionOrderOperation.Duration = articleBom.Operation.Duration * (int)quantity.GetValue();
            // Tool has no meaning yet, ignore it
            productionOrderOperation.ResourceToolId = articleBom.Operation.ResourceToolId;
            productionOrderOperation.ResourceTool = articleBom.Operation.ResourceTool;
            productionOrderOperation.ResourceSkill = articleBom.Operation.ResourceSkill;
            productionOrderOperation.ResourceSkillId = articleBom.Operation.ResourceSkillId;
            productionOrderOperation.ProducingState = ProducingState.Created;
            productionOrderOperation.ProductionOrder =
                (T_ProductionOrder)parentProductionOrder.ToIProvider();
            productionOrderOperation.ProductionOrderId =
                productionOrderOperation.ProductionOrder.Id;

            return productionOrderOperation;
        }

        public EntityCollector CreateDependingDemands(IOpenDemandManager openDemandManager, Provider provider)
        {

            EntityCollector entityCollector = new EntityCollector();    
            Demands dependingDemands = CreateProductionOrderBoms(provider.GetArticle(),
                    provider, provider.GetQuantity());
            entityCollector.AddAll(dependingDemands);
            return entityCollector;
        }
    }
}