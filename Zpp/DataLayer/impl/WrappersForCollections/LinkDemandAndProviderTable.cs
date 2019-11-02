using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Interfaces;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.ProviderDomain;

namespace Zpp.DataLayer.impl.WrappersForCollections
{
    /**
     * wraps ILinkDemandAndProvider
     */
    public class LinkDemandAndProviderTable : CollectionWrapperWithStackSet<ILinkDemandAndProvider>
    {
        private readonly Dictionary<Id, Id> _indexDemandId = new Dictionary<Id, Id>();
        private readonly Dictionary<Id, Id> _indexProviderId = new Dictionary<Id, Id>();
        
        
        public LinkDemandAndProviderTable(IEnumerable<ILinkDemandAndProvider> list) : base(list)
        {
        }

        public LinkDemandAndProviderTable()
        {
        }

        public bool Contains(Demand demand)
        {
            Id id = _indexProviderId[demand.GetId()];
            return StackSet.Contains(id);
        }

        public bool Contains(Provider provider)
        {
            Id id = _indexProviderId[provider.GetId()];
            return StackSet.Contains(id);
        }

        public ILinkDemandAndProvider GetByDemandId(Id demandId)
        {
            Id id = _indexDemandId[demandId];
            return StackSet.GetById(id);
        }
        
        public ILinkDemandAndProvider GetByProviderId(Id providerId)
        {
            Id id = _indexProviderId[providerId];
            return StackSet.GetById(id);
        }

        public override void Add(ILinkDemandAndProvider item)
        {
            if (item == null)
            {
                return;
            }

            // a set contains the element only once, else skip adding
            if (StackSet.Contains(item) == false)
            {
                _indexDemandId.Add(item.GetDemandId(), item.GetId());
                _indexProviderId.Add(item.GetProviderId(), item.GetId());
                base.Add(item);
            }
        }

        public override void Clear()
        {
            _indexDemandId.Clear();
            _indexProviderId.Clear();
            base.Clear();
        }

        public override void Remove(ILinkDemandAndProvider t)
        {
            _indexDemandId.Remove(t.GetDemandId());
            _indexProviderId.Remove(t.GetProviderId());
            base.Remove(t);
        }
    }
}