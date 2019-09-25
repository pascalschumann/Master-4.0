using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.ProviderDomain;
using Zpp.DbCache;
using Zpp.Mrp.NodeManagement;
using Zpp.WrappersForPrimitives;

namespace Zpp.Mrp.StockManagement
{
    public interface IStockManager : IProvidingManager
    {
        List<Stock> GetStocks();

        /**
         * A provider can influence the stock only once.
         * @return: providers that already have considered (=adapted the stock)
         */
        HashSet<Provider> GetAlreadyConsideredProviders();

        Provider CreateStockExchangeProvider(M_Article article, DueTime dueTime,
            Quantity demandedQuantity);

        EntityCollector AdaptStock(Provider provider);
    }
}