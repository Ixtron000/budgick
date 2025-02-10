using DataAccess.Entities;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories
{
    public class UserRepository : BaseRepository<UserEntity>, IUserRepository
    {
        public UserRepository(AppDbContext dbContext) : base(dbContext) { }

        public async Task<bool> IsAdminAsync(long userId)
        {
            return await _dbSet.AnyAsync(c => c.ChatId == userId);
        }

        public async Task<UserEntity> GetUserByUserId(long userId)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.ChatId == userId);
        }
    }
}
