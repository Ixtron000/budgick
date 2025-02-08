using DataAccess.Entities;
using DataAccess.Interfaces;

namespace DataAccess.Repositories
{
    public class UserRepository : BaseRepository<UserEntity>, IUserRepository
    {
        public UserRepository(AppDbContext dbContext) : base(dbContext) { }
    }
}
