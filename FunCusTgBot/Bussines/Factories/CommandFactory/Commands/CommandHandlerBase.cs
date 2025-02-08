using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CommandFactory.Commands
{
    public abstract class CommandHandlerBase : ICommandHandler
    {
        protected readonly string _connectionString;

        protected readonly ITelegramBotClient _botClient;
        protected readonly Update _update;

        protected CommandHandlerBase(ITelegramBotClient botClient, Update update, string connectionString)
        {
            _botClient = botClient;
            _update = update;
            _connectionString = connectionString;
        }

        protected UserCommandState CurrentStateCommand => CommandStateManager.GetCommand(_update.Message.Chat.Id);

        protected bool IsAdminUser => _update.Message.Chat.Id == 6457054702;

        public abstract Task ExecuteAsync();
    }
}
