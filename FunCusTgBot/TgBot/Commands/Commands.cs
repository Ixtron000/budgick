using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgBot.Commands
{
    public class Commands
    {
        public static async Task CommandP(ITelegramBotClient botClient, Message message)
        {
            var messageText = message.Text;
            var messagePhoto = message.Photo;
            var chatId = message.Chat.Id;

            if (true
                            //message.From.Id == 1416004677
                            ) // Проверка на администратора
            {
                var messageContent = messageText.Substring(2).Trim(); // Получаем все после '/p'

                if (string.IsNullOrWhiteSpace(messageContent) && message.Photo == null)
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы не ввели сообщение или не прикрепили изображение для отправки!");
                    return;
                }

                //using (var connection = new MySqlConnection(ConnectionString))
                //{
                //    await connection.OpenAsync();

                //    string query = "SELECT chat_id FROM users";
                //    using (var command = new MySqlCommand(query, connection))
                //    {
                //        using (var reader = await command.ExecuteReaderAsync())
                //        {
                //            while (await reader.ReadAsync())
                //            {
                var recipientChatId = 700773249;//reader["chat_id"].ToString();
                try
                {
                    if (message.Photo != null)
                    {
                        var photo = message.Photo.Last(); // Получаем последнюю (самую большую) фотографию
                        await botClient.SendPhoto(recipientChatId, photo.FileId,
                            caption: string.IsNullOrWhiteSpace(messageContent) ? null : messageContent
                        );
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId: recipientChatId, text: messageContent);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке сообщения пользователю {recipientChatId}: {ex.Message}");
                }
                //}
                //}
                //}
                //}
            }
        }
    }
}
