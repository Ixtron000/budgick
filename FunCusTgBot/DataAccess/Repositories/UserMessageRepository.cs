using Dapper;
using MySql.Data.MySqlClient;
using DataAccess.Interfaces;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories
{
    public class UserMessageRepository : IUserMessageRepository
    {
        private readonly AppDbContext _dbContext;

        public UserMessageRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddMessageAsync(UserMessageEntity message)
        {
            _dbContext.UserMessages.Add(message);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<UserMessageEntity?> GetLastMessageAsync(long userId)
        {
            return await _dbContext.UserMessages
                .Where(m => m.ChatId == userId)
                .OrderByDescending(m => m.SendDate)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateMessageAsync(UserMessageEntity message)
        {
            var existingMessage = await _dbContext.UserMessages.FindAsync(message.Id);
            if (existingMessage != null)
            {
                existingMessage.Text = message.Text;
                existingMessage.SendDate = message.SendDate;

                _dbContext.UserMessages.Update(existingMessage);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException($"Message with ID {message.Id} not found.");
            }
        }
    }
}
