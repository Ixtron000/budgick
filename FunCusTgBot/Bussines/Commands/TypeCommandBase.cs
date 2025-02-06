using Bussines.Factories.CommandFactory;
using Infrastructure.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Commands
{
    public abstract class TypeCommandBase : ITypeCommandHandler
    {
        protected readonly string _connectionString;

        protected readonly ITelegramBotClient _botClient;
        protected readonly ICommandHandler _commandHandler;
        protected readonly Update _update;
        
        public TypeCommandBase(ITelegramBotClient botClient, Update update, ICommandHandler commandHandler, string connectionstring) 
        {
            _botClient = botClient;
            _update = update;
            _connectionString = connectionstring;
            _commandHandler = commandHandler;
        }

        public abstract Task ExecuteAsync();
    }
}
