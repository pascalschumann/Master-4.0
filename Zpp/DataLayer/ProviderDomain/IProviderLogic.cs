using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.DbCache;
using Zpp.WrappersForPrimitives;

namespace Zpp.Common.ProviderDomain
{
    /**
     * A wrapper for IProvider providing methods that every wrapped ProviderType implements
     */
    public interface IProviderLogic
    {
        Demands GetAllDependingDemands();

        IProvider ToIProvider();

        Quantity GetQuantity();

        bool AnyDependingDemands();

        Id GetArticleId();
        
        M_Article GetArticle();

        bool ProvidesMoreThan(Quantity quantity);

        void CreateDependingDemands(M_Article article, Provider parentProvider, Quantity demandedQuantity);

        DueTime GetDueTime();

        Id GetId();
        
        DueTime GetStartTime();

         void SetDueTime(DueTime newDueTime);

         void SetProvided(DueTime atTime);
    }
}