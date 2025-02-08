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

        public object GetTelegramBotClient()
        {
            return _botClient;
        }
    }
}
