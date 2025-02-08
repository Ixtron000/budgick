using Autofac;
using MySql.Data.MySqlClient;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CommandFactory.Commands.PCommand
{
    public class PTextCommandHandler : CommandHandlerBase
    {
        public PTextCommandHandler(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString) :
            base(scope, botClient, update, connectionString)
        { }

        public override async Task ExecuteAsync()
        {
            var messageText = _update.Message.Caption;
            var messagePhoto = _update.Message.Photo;
            var chatId = _update.Message.Chat.Id;

            if (IsAdminUser) // Проверка на администратора
            {
                var messageContent = messageText.Substring(2).Trim(); // Получаем все после '/p'

                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    await _botClient.SendMessage(chatId, "Вы не ввели сообщение для отправки!");
                    return;
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT chat_id FROM users";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var recipientChatId = reader["chat_id"].ToString();
                                try
                                {
                                    await _botClient.SendMessage(recipientChatId, messageContent);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка при отправке сообщения пользователю {recipientChatId}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
