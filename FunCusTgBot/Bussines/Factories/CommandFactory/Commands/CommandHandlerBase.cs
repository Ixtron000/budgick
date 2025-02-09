using Autofac;
using DataAccess.Interfaces;
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

        protected readonly IUserRepository _userRepository;


        protected readonly ILifetimeScope _scope;
        protected readonly ITelegramBotClient _botClient;
        protected readonly Update _update;

        protected CommandHandlerBase(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString)
        {
            _scope = scope;

            _userRepository = scope.Resolve<IUserRepository>();

            _botClient = botClient;
            _update = update;
            _connectionString = connectionString;
        }

        protected UserCommandState CurrentStateCommand => CommandStateManager.GetCommand(_update.Message.Chat.Id);

        public abstract Task ExecuteAsync();
    }
}
