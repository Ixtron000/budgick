using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bussines.Extensions
{
    public static class UpdateExtension
    {
        /// <summary>
        /// Определяем команду из запроса
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public static string GetCommand(this Update update)
        {
            if (update is null)
            {
                throw new ArgumentNullException("Ошибка определения команды из запроса.");
            }

            if (update.Message is null) // получаем команду из колбека
            {
                return update.CallbackQuery.Data.Split(" ")[0];
            }
            if (update.Message.Type is MessageType.Photo) // получаем команду из подписи к фото
            {
                return GetCommandName(update.Message.Caption);
            }
            else
            {
                return GetCommandName(update.Message.Text);
            }
        }

        public static long GetUserId(this Update update)
        {
            if (update is null)
            {
                throw new ArgumentNullException("Ошибка определения id user из запроса.");
            }

            if (update.Message is null) // получаем id из колбека
            {
                return update.CallbackQuery.Message.Chat.Id;
            }
            else
            {
                return update.Message.Chat.Id;
            }
        }

        public static string GetMessage(this Update update)
        {
            if (update is null)
            {
                throw new ArgumentNullException("Ошибка определения сообщения из запроса.");
            }

            if (update.Message is null) // получаем сообщение из колбека
            {
                return update.CallbackQuery.Data;
            }
            if (update.Message.Type is MessageType.Photo) // получаем сообщение из подписи к фото
            {
                return update.Message.Caption;
            }
            else
            {
                return update.Message.Text;
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
                //throw new ArgumentException("Команда отсутствует или находится не в начале строки.");
                return string.Empty;
            }
        }
    }
}
