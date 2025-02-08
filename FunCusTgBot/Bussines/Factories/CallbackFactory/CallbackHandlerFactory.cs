using Autofac;
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
        public static ICallbackHandler GetHandler(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString)
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
                        return new BuyCallbackHandler(scope, botClient, update, connectionString);
                    }
                case "pay":
                    {
                        return new PayCallbackHandler(scope, botClient, update, connectionString);
                    }
                case "check":
                    {
                        return new CheckCallbackHandler(scope, botClient, update, connectionString);
                    }
                default:
                    return null;
            }
        }
    }
}
