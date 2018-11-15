using Northwind.Data.Models;
using URF.Core.Abstractions.Trackable;
using URF.Core.Services;

namespace Northwind.Service
{
    public class OrderDetailService : Service<OrderDetails>, IOrderDetailService
    {
        public OrderDetailService(ITrackableRepository<OrderDetails> repository) : base(repository)
        {
        }
    }
}
