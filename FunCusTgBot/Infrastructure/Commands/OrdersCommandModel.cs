using Infrastructure.Enums;
using Infrastructure.Models;

namespace Infrastructure.Commands
{
    public class OrdersCommandModel
    {
        public List<Order> CurrentOrders { get; set; }
        public List<Order> UserOrders { get; set; }

        public OrdersCommandState State { get; set; }
    }
}
