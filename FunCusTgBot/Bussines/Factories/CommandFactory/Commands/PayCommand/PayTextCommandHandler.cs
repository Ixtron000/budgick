using Bussines.Factories.CallbackFactory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CommandFactory.Commands.PayCommand
{
    public class PayTextCommandHandler : CommandHandlerBase
    {
        public PayTextCommandHandler(ITelegramBotClient botClient, Update update, string connectionString) :
            base(botClient, update, connectionString)
        {
        }


        public async override Task ExecuteAsync()
        {
            var callbackHandler = CallbackHandlerFactory.GetHandler(_botClient, _update, _connectionString);
            await callbackHandler.ExecuteAsync();
        }
    }
}
