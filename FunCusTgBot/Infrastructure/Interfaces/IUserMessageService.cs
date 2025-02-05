using Infrastructure.Models;

namespace Infrastructure.Interfaces
{
    public interface IUserMessageService
    {
        Task AddMessageAsync(UserMessage message);
        Task<UserMessage> GetLastMessageAsync(long userId);
        Task UpdateMessageAsync(UserMessage message);
    }
}
