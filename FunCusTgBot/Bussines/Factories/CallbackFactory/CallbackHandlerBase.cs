using Autofac;
using Bussines.Extensions;
using Bussines.Factories.CommandFactory;
using Bussines.Services;
using DataAccess.Interfaces;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CallbackFactory
{
    public abstract class CallbackHandlerBase : ICallbackHandler
    {
        protected readonly FreeKassaService _freeKassaService = new FreeKassaService();
        protected readonly IUserRepository _userRepository;
        protected readonly ITelegramBotClient _botClient;
        protected readonly Update _update;

        protected CallbackHandlerBase(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString)
        {
            _botClient = botClient;
            _update = update;

            _userRepository = scope.Resolve<IUserRepository>();

            InitCommandState();

            UserId = update.GetUserId();
            CurrentStateCommand = CommandStateManager.GetCommand(UserId);
            Message = update.GetMessage();
            ConnectionString = connectionString;
        }

        protected UserCommandState CurrentStateCommand { get; }

        protected long UserId { get; }

        protected string Command => CurrentStateCommand.Command;
        
        protected string Message { get; }

        protected string ConnectionString { get; }

        public abstract Task ExecuteAsync();

        private void InitCommandState()
        {
            var userId = _update.GetUserId();
            if (!CommandStateManager.IsExistsState(userId))
            {
                var userStateCommand = UserCommandState.Create(userId, _update.GetCommand());
                CommandStateManager.AddCommand(userStateCommand);
            }
        }
    }
}
