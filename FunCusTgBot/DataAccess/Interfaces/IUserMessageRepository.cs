using DataAccess.Entities;

namespace DataAccess.Interfaces
{
    public interface IUserMessageRepository
    {
        Task AddMessageAsync(UserMessageEntity message);
        Task<UserMessageEntity> GetLastMessageAsync(long userId);
        Task UpdateMessageAsync(UserMessageEntity message);
    }
}
