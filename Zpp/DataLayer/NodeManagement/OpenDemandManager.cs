using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.DbCache;
using Zpp.Utils;
using Zpp.WrappersForCollections;

namespace Zpp.Mrp.NodeManagement
{
    public class OpenDemandManager : IOpenDemandManager
    {
        // This is only for remembering already added demands !!! (not to persist or anything else)
        private readonly IDemands _demands = new Demands();

        private readonly OpenNodes<Demand> _openDemands = new OpenNodes<Demand>();

        // 
        private readonly IProviderToDemandTable
            _providerToDemandTable = new ProviderToDemandTable();

        public void AddDemand(Id providerId, Demand oneDemand, Quantity reservedQuantity)
        {
            if (_demands.GetDemandById(oneDemand.GetId()) != null)
            {
                throw new MrpRunException("You cannot add an already added demand.");
            }


            // if it has quantity that is not reserved, remember it for later reserving
            if (oneDemand.GetType() == typeof(StockExchangeDemand) &&
                reservedQuantity.IsSmallerThan(oneDemand.GetQuantity()))
            {
                _openDemands.Add(oneDemand.GetArticle(),
                    new OpenNode<Demand>(oneDemand, oneDemand.GetQuantity().Minus(reservedQuantity),
                        oneDemand.GetArticle()));
            }

            // save demand
            _demands.Add(oneDemand);

            T_ProviderToDemand providerToDemand = new T_ProviderToDemand();
            providerToDemand.DemandId = oneDemand.GetId().GetValue();
            providerToDemand.ProviderId = providerId.GetValue();
            providerToDemand.Quantity = reservedQuantity.GetValue();

            _providerToDemandTable.Add(providerToDemand);
        }

        /**
         * aka ReserveQuantityOfExistingDemand or satisfyByAlreadyExistingDemand
         */
        public ResponseWithDemands SatisfyProviderByOpenDemand(Provider provider,
            Quantity demandedQuantity, IDbTransactionData dbTransactionData)
        {
            if (_openDemands.AnyOpenProvider(provider.GetArticle()))
            {

                foreach (var openDemand in _openDemands.GetOpenProvider(provider.GetArticle()))
                {
                    if (openDemand != null && provider.GetDueTime()
                            .IsGreaterThanOrEqualTo(openDemand.GetOpenNode().GetDueTime()))
                    {
                        Quantity remainingQuantity =
                            demandedQuantity.Minus(openDemand.GetOpenQuantity());
                        openDemand.GetOpenQuantity().DecrementBy(demandedQuantity);

                        if (openDemand.GetOpenQuantity().IsNegative() ||
                            openDemand.GetOpenQuantity().IsNull())
                        {
                            _openDemands.Remove(openDemand);
                        }

                        if (remainingQuantity.IsNegative())
                        {
                            remainingQuantity = Quantity.Null();
                        }

                        T_ProviderToDemand providerToDemand = new T_ProviderToDemand()
                        {
                            ProviderId = provider.GetId().GetValue(),
                            DemandId = openDemand.GetOpenNode().GetId().GetValue(),
                            Quantity = demandedQuantity.Minus(remainingQuantity).GetValue()
                        };

                        return new ResponseWithDemands(null, providerToDemand, demandedQuantity);
                    }
                }
            }

            return new ResponseWithDemands((Demand) null, null, demandedQuantity);
            
        }
    }
}