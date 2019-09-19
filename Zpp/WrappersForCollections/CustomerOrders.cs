using System.Collections.Generic;
using Master40.DB.DataModel;
using Zpp.WrapperForEntities;

namespace Zpp.WrappersForCollections
{
    public class CustomerOrders : CollectionWrapperWithList<T_CustomerOrder>
    {
        public CustomerOrders(List<T_CustomerOrder> list) : base(list)
        {
        }

        public CustomerOrders()
        {
        }

        public List<T_CustomerOrder> GetAllAsTCustomerOrders()
        {
            List<T_CustomerOrder> customerOrders = new List<T_CustomerOrder>();
            foreach (var customerOrder in this.List)
            {
                customerOrders.Add(customerOrder);
            }
            return customerOrders;
        }
    }
}