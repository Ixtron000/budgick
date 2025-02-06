using Bussines.Extensions;
using Bussines.Factories.CallbackFactory.Callbacks;
using Bussines.Factories.CommandFactory;
using Infrastructure.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CallbackFactory
{
    public static class CallbackHandlerFactory
    {
        public static ICallbackHandler GetHandler(ITelegramBotClient botClient, Update update, string connectionString)
        {
            var userId = update.GetUserId();
            var commandName = string.Empty;

            if (CommandStateManager.IsExistsState(userId))
            {
                commandName = CommandStateManager.GetCommand(userId).Command;
            }
            else
            {
                commandName = update.GetCommand();
            }

            switch (commandName)
            {
                case "buy":
                    {
                        return new BuyCallbackHandler(botClient, update, connectionString);
                    }
                default:
                    return null;
            }
        }
    }
}
