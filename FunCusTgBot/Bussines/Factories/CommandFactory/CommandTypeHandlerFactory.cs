using Bussines.Commands;
using Bussines.Extensions;
using Bussines.Factories.CommandFactory.Commands.BuyCommand;
using Bussines.Factories.CommandFactory.Commands.PCommand;
using Infrastructure.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bussines.Factories.CommandFactory
{
    public static class CommandTypeHandlerFactory
    {
        public static ITypeCommandHandler GetHandler(ITelegramBotClient botClient, Update update, string connectionString)
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
                            commandhandler = new PTextCommandHandler(botClient, update, connectionString);
                        }
                        else if (commandName == "buy")
                        {
                            commandhandler = new BuyTextCommandHandler(botClient, update, connectionString);
                        }
                        else if (commandName == "pay")
                        {
                            commandhandler = new BuyTextCommandHandler(botClient, update, connectionString);
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
                            commandhandler = new PPhotoCommandHandler(botClient, update, connectionString);
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
