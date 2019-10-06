using System.Collections.Generic;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.Util.Graph;

namespace Zpp.Production.impl.ProductionTypes
{
    public class ProductionOrderOperationGraphsAsDictionary : Dictionary<ProductionOrder, IDirectedGraph<INode>>
    {
    }
}
