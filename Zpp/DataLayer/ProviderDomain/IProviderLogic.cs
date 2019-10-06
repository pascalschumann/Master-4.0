using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;

namespace Zpp.DataLayer.ProviderDomain
{
    /**
     * A wrapper for IProvider providing methods that every wrapped ProviderType implements
     */
    public interface IProviderLogic
    {

        IProvider ToIProvider();

        Id GetArticleId();
        
        M_Article GetArticle();

        bool ProvidesMoreThan(Quantity quantity);
        
        DueTime GetDueTime();

        Id GetId();
        
        DueTime GetStartTime();

        void SetProvided(DueTime atTime);
    }
}