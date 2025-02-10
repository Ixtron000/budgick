using DataAccess.Entities;
using DataAccess.Interfaces;
using Infrastructure.Interfaces;
using Infrastructure.Models;

namespace Bussines.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;

        public OrderService(IUserRepository userRepository, IOrderRepository orderRepository) 
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
        }

        public async Task CreateOrUpdateStatusOrder(long userId, Order order)
        {
            var orderEntity = await _orderRepository.GetByIdAsync(order.Id);
            
            if (orderEntity is not null)
            {
                orderEntity.Status = order.Status;
                await _orderRepository.UpdateAsync(orderEntity);
            }
            else
            {
                var userEntity = await _userRepository.GetUserByUserId(userId);
                var newOrderEntity = new OrderEntity()
                {
                    Id = order.Id,
                    Status = order.Status,
                    Amount= order.Amount,
                    Date = order.Date,
                    UserId = userEntity.Id
                };

                await _orderRepository.AddAsync(newOrderEntity);
            }
        }
    }
}
