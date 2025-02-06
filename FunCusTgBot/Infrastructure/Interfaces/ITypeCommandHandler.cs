using Telegram.Bot;
using Telegram.Bot.Types;

namespace Infrastructure.Interfaces
{
    public interface ITypeCommandHandler
    {
        Task ExecuteAsync();
    }
}
