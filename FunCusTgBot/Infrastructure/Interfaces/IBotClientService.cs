using Telegram.Bot.Types;

namespace Infrastructure.Interfaces
{
    public interface IBotClientService
    {
        Task<Update> GetLastMessage();
        object GetTelegramBotClient();
    }
}
