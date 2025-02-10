using Autofac;
using Bussines.Commands;
using Bussines.Extensions;
using Bussines.Factories.CommandFactory.Commands.BuyCommand;
using Bussines.Factories.CommandFactory.Commands.CheckCommand;
using Bussines.Factories.CommandFactory.Commands.OrdersCommand;
using Bussines.Factories.CommandFactory.Commands.PCommand;
using Infrastructure.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bussines.Factories.CommandFactory
{
    public static class CommandTypeHandlerFactory
    {
        public static ITypeCommandHandler GetHandler(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString)
        {
            ICommandHandler commandhandler = null;
            var messageType = update.Message.Type;
            var userId = update.GetUserId();
            var commandName = string.Empty;

            // проверяем находится ли пользователь в состоянии машине состоянии команды
            if (CommandStateManager.IsExistsState(userId))
            {
                commandName = CommandStateManager.GetCommand(userId).Command;
            }
            else
            {
                commandName = update.GetCommand();
            }

            switch (messageType)
            {
                case MessageType.Text:
                    {
                        if (commandName == "p")
                        {
                            commandhandler = new PTextCommandHandler(scope, botClient, update, connectionString);
                        }
                        else if (commandName == "buy")
                        {
                            commandhandler = new BuyTextCommandHandler(scope, botClient, update, connectionString);
                        }
                        else if (commandName == "pay")
                        {
                            commandhandler = new BuyTextCommandHandler(scope, botClient, update, connectionString);
                        }
                        else if (commandName == "check")
                        {
                            commandhandler = new CheckTextCommandHandler(scope, botClient, update, connectionString);
                        }
                        else if (commandName == "myorders")
                        {
                            commandhandler = new OrdersTextCommandHandler(scope, botClient, update, connectionString);
                        }
                        else
                        {
                            return null;
                        }

                        return new TextTypeCommand(botClient, update, commandhandler, connectionString);
                    }
                case MessageType.Photo:
                    {
                        if (commandName == "p")
                        {
                            commandhandler = new PPhotoCommandHandler(scope, botClient, update, connectionString);
                        }
                        else
                        {
                            return null;
                        }

                        return new PhotoTypeCommand(botClient, update, commandhandler, connectionString);
                    }
                default:
                    Console.WriteLine("Реализация команды данного типа сообщения отсутствует.");
                    return null;
            }
        }
    }
}
