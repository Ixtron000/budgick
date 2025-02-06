using Bussines.Extensions;
using Bussines.Factories.CommandFactory;
using Infrastructure.Commands;
using Infrastructure.Enums;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CallbackFactory
{
    public abstract class CallbackHandlerBase : ICallbackHandler
    {
        protected readonly ITelegramBotClient _botClient;
        protected readonly Update _update;

        protected CallbackHandlerBase(ITelegramBotClient botClient, Update update, string connectionString)
        {
            _botClient = botClient;
            _update = update;

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
                var command = new BuyCommandModel()
                {
                    State = BuyCommandState.None
                };

                var userStateCommand = UserCommandState.Create(userId, _update.GetCommand(), command);
                CommandStateManager.AddCommand(userStateCommand);
            }
        }
    }
}
