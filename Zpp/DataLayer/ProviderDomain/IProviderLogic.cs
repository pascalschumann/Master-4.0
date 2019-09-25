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

        IProvider ToIProvider();

        Quantity GetQuantity();

        Id GetArticleId();
        
        M_Article GetArticle();

        bool ProvidesMoreThan(Quantity quantity);
        
        DueTime GetDueTime();

        Id GetId();
        
        DueTime GetStartTime();

        void SetProvided(DueTime atTime);
    }
}