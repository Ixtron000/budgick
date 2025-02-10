using Infrastructure.Models;

namespace Infrastructure.Interfaces
{
    public interface IOrderService
    {
        Task CreateOrUpdateStatusOrder(long userId, Order order);
    }
}
