using Bussines.Factories.CommandFactory;
using Infrastructure.Enums;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bussines.Factories.CallbackFactory.Callbacks
{
    public class BuyCallbackHandler : CallbackHandlerBase
    {
        private readonly HttpClient HttpClient = new HttpClient();

        public BuyCallbackHandler(ITelegramBotClient botClient, Update update, string connectionString) :
            base (botClient, update, connectionString)
        {
        }

        public override async Task ExecuteAsync()
        {
            if (CurrentStateCommand.BuyCommand.State is BuyCommandState.None)
            {
                var msg = "Укажите количество.";

                // параметр переданный в колбеке
                var srvId = Message.Split(" ")[1];
                if (long.TryParse(srvId, out long serviceId))
                {
                    // поставил на ожидание получения количества

                    // параметр переданный в колбеке
                    CurrentStateCommand.BuyCommand.ServiceId = serviceId;
                    CurrentStateCommand.BuyCommand.State = BuyCommandState.ChooseCount;
                    CommandStateManager.AddCommand(CurrentStateCommand);
                }
                else
                {
                    msg = "Id услуги должно содержать только цифры.";
                }

                await _botClient.SendMessage(UserId, "Укажите количество.");
                return;
            }

            if (CurrentStateCommand.BuyCommand.State is BuyCommandState.ChooseCount)
            {
                var msg = "Укажите ссылку.";
                if (int.TryParse(Message, out int count))
                {
                    // обновил команду и поставил на ожидание получения ссылки
                    CurrentStateCommand.BuyCommand.Count = count;
                    CurrentStateCommand.BuyCommand.State = BuyCommandState.SendLink;
                    CommandStateManager.AddCommand(CurrentStateCommand);
                }
                else
                {
                    msg = "Количество указывается в цифрах.";
                }

                await _botClient.SendMessage(UserId, msg);
                return;
            }

            if (CurrentStateCommand.BuyCommand.State is BuyCommandState.SendLink)
            {
                var msg = "Формирование покупки.";
                if (IsValidUrl(Message))
                {
                    // обновил команду
                    CurrentStateCommand.BuyCommand.Link = Message;
                    CommandStateManager.AddCommand(CurrentStateCommand);
                }
                else
                {
                    msg = "Пришлите ссылку.";
                    await _botClient.SendMessage(UserId, msg);
                    return;
                }

                await _botClient.SendMessage(UserId, msg);
            }

            bool hasError = false;

            if (CurrentStateCommand.BuyCommand.State is BuyCommandState.SendLink)
            {
                try
                {
                    HttpResponseMessage response = await HttpClient.GetAsync("https://soc-rocket.ru/api/v2/?action=services&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
                    response.EnsureSuccessStatusCode();

                    string responseBody1 = await response.Content.ReadAsStringAsync();
                    JArray jsonArray = JArray.Parse(responseBody1);
                    var service = jsonArray.FirstOrDefault(s => s["service"]?.ToString() == CurrentStateCommand.BuyCommand.ServiceId.ToString());

                    if (service != null)
                    {
                        decimal rate = service["rate"].Value<decimal>();
                        decimal price = rate * 2;
                        Console.WriteLine($"Rate: {rate}, Price: {price}");

                        try
                        {
                            using (var connection = new MySqlConnection(ConnectionString))
                            {
                                await connection.OpenAsync();

                                string query = "SELECT balance FROM users WHERE chat_id = @chatId";
                                using (var command = new MySqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@chatId", UserId);

                                    var balanceObj = await command.ExecuteScalarAsync();
                                    if (balanceObj != null && decimal.TryParse(balanceObj.ToString(), out decimal balance))
                                    {
                                        var partsValue = CurrentStateCommand.BuyCommand.Count;

                                        decimal amountToDeduct = (price / 1000m) * partsValue;
                                        string formattedAmountToDeduct = amountToDeduct.ToString("0.0");
                                        Console.WriteLine($"Balance: {balance}, Amount to Deduct: {amountToDeduct}");
                                        if (balance >= amountToDeduct)
                                        {
                                            // Выполнение заказа
                                            string responseBody = await HttpClient.GetStringAsync($"https://soc-rocket.ru/api/v2/?action=add&service={CurrentStateCommand.BuyCommand.ServiceId.ToString()}&link={CurrentStateCommand.BuyCommand.Link}&quantity={CurrentStateCommand.BuyCommand.Count}&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
                                            JObject jsonResponse = JObject.Parse(responseBody);
                                            if (jsonResponse.ContainsKey("order"))
                                            {
                                                var orderId = jsonResponse["order"].ToString();
                                                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                                {
                                                        new[]
                                                        {
                                                            InlineKeyboardButton.WithCallbackData("🔙 Главная","main")
                                                        }
                                                    });

                                                string statusResponseBody = await HttpClient.GetStringAsync($"https://soc-rocket.ru/api/v2/?action=status&orders={orderId}&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
                                                JObject statusResponse = JObject.Parse(statusResponseBody);

                                                if (statusResponse.ContainsKey(orderId))
                                                {
                                                    var orderInfo = statusResponse[orderId];
                                                    decimal charge = orderInfo["charge"].Value<decimal>();
                                                    string statusMessage = $"🚀✨ Заказ №{orderId} успешно создан! 🎉🥳" +
                                                        $"\n" +
                                                        $"📝  Информация о заказе {orderId}:\n\n" +
                                                        $"🔴 Стоимость: {charge * 2} {orderInfo["currency"]}\n" +
                                                        $"🔹 ID: {orderInfo["service"]}\n" +
                                                        $"🌐 Ссылка: {orderInfo["link"]}\n" +
                                                        $"📦 Количество: {orderInfo["quantity"]}\n" +
                                                        $"📊 Начальное количество: {orderInfo["start_count"]}\n" +
                                                        $"📅 Дата: {orderInfo["date"]}\n" +
                                                        $"✅ Статус: {orderInfo["status"]}\n" +
                                                        $"📦 Остаток: {orderInfo["remains"]}\n\n 💚 Для получения информации о заказе: \n/status {orderId}";

                                                    await _botClient.SendMessage(UserId, statusMessage, replyMarkup: inlineKeyboard);

                                                    // Update user balance
                                                    string updateQuery = "UPDATE users SET balance = @newBalance WHERE user_id = @chatId";
                                                    using (var updateCommand = new MySqlCommand(updateQuery, connection))
                                                    {
                                                        updateCommand.Parameters.AddWithValue("@newBalance", balance - amountToDeduct);
                                                        updateCommand.Parameters.AddWithValue("@chatId", UserId);
                                                        await updateCommand.ExecuteNonQueryAsync();
                                                    }
                                                }
                                                else if (statusResponse.ContainsKey("error"))
                                                {
                                                    hasError = true;
                                                    string errorMessage = $"Ошибка при получении статуса заказа {orderId}: {statusResponse["error"]}";
                                                    await _botClient.SendTextMessageAsync(UserId, errorMessage);
                                                }
                                            }
                                            else if (jsonResponse.ContainsKey("error"))
                                            {
                                                hasError = true;
                                                await _botClient.SendMessage(UserId, $"Ошибка: {jsonResponse["error"]}");
                                            }
                                            else
                                            {
                                                hasError = true;
                                                Console.WriteLine("Неизвестный ответ от сервера");
                                            }
                                        }
                                        else
                                        {
                                            var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                            {
                                                    new[]
                                                    {
                                                        InlineKeyboardButton.WithCallbackData("🔙 Главная","main")
                                                    }
                                                });
                                            await _botClient.SendMessage(UserId, $"❌ У вас не хватает средств на балансе! ❌" +
                                                "\r\n\n" +
                                                $"💚Ваш баланс: {balance} ₽\n" +
                                                $"💛Требуется к оплате: {formattedAmountToDeduct} ₽\n\n" +
                                                $"💥Для пополнения баланса напишите /balance!", replyMarkup: inlineKeyboard);

                                            // удаляем команду при завершении оформления заказа.
                                            // удалять обязательно, потому что словарь можно наполниться до огромных размеров
                                            CommandStateManager.DeleteCommand(UserId);
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        await _botClient.SendMessage(UserId, "Произошла ошибка. 😔");
                                        hasError = true;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при заказе: {ex.Message}");
                            hasError = true;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Ошибка запроса: {ex.Message}");
                    hasError = true;
                }
            }

            var endMsg = "Заказ сформирован.";
            var keyBrd = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "main") });

            if (hasError)
            {
                endMsg = "Произошла ошибка. 😔";
            }

            // удаляем команду при завершении оформления заказа.
            // удалять обязательно, потому что словарь можно наполниться до огромных размеров
            CommandStateManager.DeleteCommand(UserId);

            await _botClient.SendMessage(UserId, endMsg, replyMarkup: keyBrd);
        }

        public static bool IsValidUrl(string url)
        {
            // Проверяем, чтобы строка не была пустой
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            // Пробуем создать Uri
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
