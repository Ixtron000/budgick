using Infrastructure.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Services
{
    public class BotClientService : IBotClientService
    {
        private readonly TelegramBotClient _botClient;

        public BotClientService(TelegramBotClient botClient) 
        {
            _botClient = botClient;
        }

        public async Task<Update> GetLastMessage()
        {
            var updates = await _botClient.GetUpdatesAsync();
            var lastUpdate = updates.OrderByDescending(t => t.Message.Date).ToList();
            return lastUpdate[1];
        }

        public object GetTelegramBotClient()
        {
            return _botClient;
        }
    }
}
