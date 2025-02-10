using DataAccess.Entities;

namespace DataAccess.Interfaces
{
    public interface IUserRepository : IBaseRepository<UserEntity>
    {
        Task<UserEntity> GetUserByUserId(long userId);
        Task<bool> IsAdminAsync(long userId);
    }
}
