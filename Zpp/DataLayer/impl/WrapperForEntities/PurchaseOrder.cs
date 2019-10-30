using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;

namespace Zpp.DataLayer.impl.WrapperForEntities
{
    /**
     * wraps T_PurchaseOrder
     */
    public class PurchaseOrder: IId
    {
        private T_PurchaseOrder _purchaseOrder;

        public PurchaseOrder(T_PurchaseOrder purchaseOrder)
        {
            _purchaseOrder = purchaseOrder;
        }

        public PurchaseOrder()
        {
        }

        public T_PurchaseOrder ToT_PurchaseOrder()
        {
            return _purchaseOrder;
        }

        public Id GetId()
        {
            return _purchaseOrder.GetId();
        }

        public string AsString()
        {
            return _purchaseOrder.AsString();
        }
    }
}