using System.Collections.Generic;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.Util.Graph;

namespace Zpp.Mrp2.impl.Mrp1.impl.Production.impl.ProductionTypes
{
    public class ProductionOrderOperationGraphsAsDictionary : Dictionary<ProductionOrder, IDirectedGraph<INode>>
    {
    }
}
