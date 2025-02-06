using Bussines.Commands;
using Bussines.Factories.CommandFactory.Commands.BuyCommand;
using Bussines.Factories.CommandFactory.Commands.PCommand;
using Infrastructure.Interfaces;
using System.Text.RegularExpressions;
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
            var userId = update.Message.Chat.Id;

            switch (messageType)
            {
                case MessageType.Text:
                    {
                        string commandText = string.Empty;
                        
                        // проверяем находится ли пользователь в состоянии машине состоянии команды
                        if (CommandStateManager.IsExistsState(userId))
                        {
                            commandText = CommandStateManager.GetCommand(userId).Command;
                        }
                        else
                        {
                            commandText = GetCommandName(update.Message.Text);
                        }


                        if (commandText == "p")
                        {
                            commandhandler = new PTextCommandHandler(botClient, update, connectionString);
                        }
                        else if (commandText == "buy")
                        {
                            commandhandler = new BuyTextCommandHandler(botClient, update, connectionString);
                        }
                        else
                        {
                            Console.WriteLine("Реализация команды данного типа сообщения отсутствует.");
                            return null;
                        }

                        return new TextTypeCommand(botClient, update, commandhandler, connectionString);
                    }
                case MessageType.Photo:
                    {
                        var commandText = GetCommandName(update.Message.Caption);

                        if (commandText == "p")
                        {
                            commandhandler = new PPhotoCommandHandler(botClient, update, connectionString);
                        }
                        else
                        {
                            Console.WriteLine("Реализация команды данного типа сообщения отсутствует.");
                            return null;
                        }

                        return new PhotoTypeCommand(botClient, update, commandhandler, connectionString);
                    }
                default:
                    Console.WriteLine("Реализация команды данного типа сообщения отсутствует.");
                    return null;
            }
        }

        private static string GetCommandName(string commandText)
        {
            // Регулярное выражение для команды в начале строки
            Regex commandRegex = new Regex(@"^/(?<command>\w+)");

            // Попытка извлечь команду
            Match match = commandRegex.Match(commandText);
            if (match.Success)
            {
                string command = match.Groups["command"].Value;
                return command;
            }
            else
            {
                throw new ArgumentException("Команда отсутствует или находится не в начале строки.");
            }
        }
    }
}
