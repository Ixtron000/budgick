using Autofac;
using Bussines.Factories.CallbackFactory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CommandFactory.Commands.CheckCommand
{
    public class CheckTextCommandModel : CommandHandlerBase
    {
        public CheckTextCommandModel(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString) :
            base(scope, botClient, update, connectionString)
        {
        }

        public async override Task ExecuteAsync()
        {
            var callbackHandler = CallbackHandlerFactory.GetHandler(_scope, _botClient, _update, _connectionString);
            await callbackHandler.ExecuteAsync();
        }
    }
}
