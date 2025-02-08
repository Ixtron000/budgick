using Telegram.Bot.Types;

namespace Infrastructure.Interfaces
{
    public interface IBotClientService
    {
        object GetTelegramBotClient();
    }
}
