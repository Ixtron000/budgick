using Telegram.Bot;
using Telegram.Bot.Types;
using MySql.Data.MySqlClient;
using Autofac;

namespace Bussines.Factories.CommandFactory.Commands.PCommand
{
    public class PPhotoCommandHandler : CommandHandlerBase
    {
        public PPhotoCommandHandler(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString) : 
            base(scope, botClient, update, connectionString)
        { }

        public override async Task ExecuteAsync()
        {
            var messageText = _update.Message.Caption;
            var messagePhoto = _update.Message.Photo;
            var chatId = _update.Message.Chat.Id;

            if (IsAdminUser)
            {
                string messageContent = string.Empty;
                if (!string.IsNullOrEmpty(messageText))
                {
                    messageContent = messageText.Substring(2).Trim(); // Получаем все после '/p'
                }

                if (string.IsNullOrWhiteSpace(messageContent) && messagePhoto == null)
                {
                    await _botClient.SendMessage(chatId, "Вы не ввели сообщение или не прикрепили изображение для отправки!");
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
                                    var photo = messagePhoto.Last(); // Получаем последнюю (самую большую) фотографию
                                    await _botClient.SendPhoto(recipientChatId, photo.FileId,
                                        caption: string.IsNullOrWhiteSpace(messageContent) ? null : messageContent
                                    );
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