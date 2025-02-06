using Infrastructure.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Commands
{
    public class PhotoTypeCommand : TypeCommandBase
    {
        public PhotoTypeCommand(ITelegramBotClient botClient, Update update, ICommandHandler commandHandler, string connectionString) 
            : base(botClient, update, commandHandler, connectionString)
        { }

        public override async Task ExecuteAsync()
        {
            await _commandHandler.ExecuteAsync();
        }
    }
}
