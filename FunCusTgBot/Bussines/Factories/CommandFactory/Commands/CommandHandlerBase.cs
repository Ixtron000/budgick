using Infrastructure.Interfaces;
using Infrastructure.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CommandFactory.Commands
{
    public abstract class CommandHandlerBase : ICommandHandler
    {
        protected readonly string _connectionString;

        protected readonly ITelegramBotClient _botClient;
        protected readonly Update _update;

        public CommandHandlerBase(ITelegramBotClient botClient, Update update, string connectionstring)
        {
            _botClient = botClient;
            _update = update;
            _connectionString = connectionstring;
        }

        protected UserCommandState CurrentStateCommand => CommandStateManager.GetCommand(_update.Message.Chat.Id);

        public abstract Task ExecuteAsync();
    }
}
