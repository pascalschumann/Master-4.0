using Priority_Queue;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.Util;
using Zpp.Util.Queue;

namespace Zpp.Mrp2.impl.Mrp1.impl
{
    public class Mrp1 : IMrp1
    {
        private readonly IDemands dbDemands;

        public Mrp1(IDemands dbDemands)
        {
            this.dbDemands = dbDemands;
        }

        public void StartMrp1()
        {
            // init
            int MAX_DEMANDS_IN_QUEUE = 100000;

            FastPriorityQueue<DemandQueueNode> demandQueue =
                new FastPriorityQueue<DemandQueueNode>(MAX_DEMANDS_IN_QUEUE);

            IProviderManager providerManager = new ProviderManager();

            foreach (var demand in dbDemands)
            {
                // TODO: EnqueueAll()
                demandQueue.Enqueue(new DemandQueueNode(demand), demand.GetStartTime().GetValue());
            }
            
            EntityCollector allCreatedEntities = new EntityCollector();
            while (demandQueue.Count != 0)
            {
                DemandQueueNode firstDemandInQueue = demandQueue.Dequeue();

                EntityCollector response =
                    MaterialRequirementsPlanningForOneDemand(firstDemandInQueue.GetDemand(), providerManager);
                allCreatedEntities.AddAll(response);

                // TODO: EnqueueAll()
                foreach (var demand in response.GetDemands())
                {
                    demandQueue.Enqueue(new DemandQueueNode(demand),
                        demand.GetStartTime().GetValue());
                }
            }

            // write data to _dbTransactionData
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            dbTransactionData.AddAll(allCreatedEntities);
            // End of MaterialRequirementsPlanning
        }
        
        private EntityCollector MaterialRequirementsPlanningForOneDemand(Demand demand,
            IProviderManager providerManager)
        {
            EntityCollector entityCollector = new EntityCollector();

            EntityCollector response = providerManager.Satisfy(demand, demand.GetQuantity());
            entityCollector.AddAll(response);
            providerManager.AdaptStock(response.GetProviders());
            response = providerManager.CreateDependingDemands(entityCollector.GetProviders());
            entityCollector.AddAll(response);

            if (entityCollector.IsSatisfied(demand) == false)
            {
                throw new MrpRunException($"'{demand}' was NOT satisfied: remaining is " +
                                          $"{entityCollector.GetRemainingQuantity(demand)}");
            }

            return entityCollector;
        }
    }
}