using Infrastructure.Enums;
using Infrastructure.Models;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Infrastructure.Commands;

namespace Bussines.Factories.CommandFactory.Commands.BuyCommand
{
    public class BuyTextCommandHandler : CommandHandlerBase
    {
        private readonly HttpClient HttpClient = new HttpClient();

        public BuyTextCommandHandler(ITelegramBotClient botClient, Update update, string connectionString) :
            base(botClient, update, connectionString)
        {
            var userId = _update.Message.Chat.Id;
            if (!CommandStateManager.IsExistsState(userId))
            {
                var command = new BuyCommandModel()
                {
                    State = BuyCommandState.None
                };

                var userStateCommand = UserCommandState.Create(userId, "buy", command);
                CommandStateManager.AddCommand(userStateCommand);
            }
        }

        public override async Task ExecuteAsync()
        {
            var messageText = _update.Message.Text;
            var chatId = _update.Message.Chat.Id;

            if (CurrentStateCommand.BuyCommand.State is BuyCommandState.None)
            {
                await _botClient.SendMessage(chatId, "Укажите id услуги.");

                // обновил на ожидание id услуги
                CurrentStateCommand.BuyCommand.State = BuyCommandState.ChooseServiceId;
                CommandStateManager.AddCommand(CurrentStateCommand);
                return;
            }

            if (CurrentStateCommand.BuyCommand.State is BuyCommandState.ChooseServiceId)
            {
                var msg = "Укажите количество.";
                if (long.TryParse(messageText, out long serviceId))
                {
                    // обновил команду и поставил на ожидание получения количества
                    CurrentStateCommand.BuyCommand.ServiceId = serviceId;
                    CurrentStateCommand.BuyCommand.State = BuyCommandState.ChooseCount;
                    CommandStateManager.AddCommand(CurrentStateCommand);
                }
                else
                {
                    msg = "Id услуги должно содержать только цифры.";
                }

                await _botClient.SendMessage(chatId, "Укажите количество.");
                return;
            }

            if (CurrentStateCommand.BuyCommand.State is BuyCommandState.ChooseCount)
            {
                var msg = "Укажите ссылку.";
                if (int.TryParse(messageText, out int count))
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

                await _botClient.SendMessage(chatId, msg);
                return;
            }

            if (CurrentStateCommand.BuyCommand.State is BuyCommandState.SendLink)
            {
                var msg = "Формирование покупки.";
                if (IsValidUrl(messageText))
                {
                    // обновил команду
                    CurrentStateCommand.BuyCommand.Link = messageText;
                    CommandStateManager.AddCommand(CurrentStateCommand);
                }
                else
                {
                    msg = "Пришлите ссылку.";
                    await _botClient.SendMessage(chatId, msg);
                    return;
                }

                await _botClient.SendMessage(chatId, msg);
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

                        using (var connection = new MySqlConnection("Server=127.0.0.1;Database=budguck;User=root;Password=;"))
                        {
                            await connection.OpenAsync();

                            string query = "SELECT balance FROM users WHERE chat_id = @chatId";
                            using (var command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@chatId", chatId);

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

                                                await _botClient.SendTextMessageAsync(chatId, statusMessage, replyMarkup: inlineKeyboard);

                                                // Update user balance
                                                string updateQuery = "UPDATE users SET balance = @newBalance WHERE user_id = @chatId";
                                                using (var updateCommand = new MySqlCommand(updateQuery, connection))
                                                {
                                                    updateCommand.Parameters.AddWithValue("@newBalance", balance - amountToDeduct);
                                                    updateCommand.Parameters.AddWithValue("@chatId", chatId);
                                                    await updateCommand.ExecuteNonQueryAsync();
                                                }
                                            }
                                            else if (statusResponse.ContainsKey("error"))
                                            {
                                                hasError = true;
                                                string errorMessage = $"Ошибка при получении статуса заказа {orderId}: {statusResponse["error"]}";
                                                await _botClient.SendTextMessageAsync(chatId, errorMessage);
                                            }
                                        }
                                        else if (jsonResponse.ContainsKey("error"))
                                        {
                                            hasError = true;
                                            await _botClient.SendTextMessageAsync(chatId, $"Ошибка: {jsonResponse["error"]}");
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
                                        await _botClient.SendMessage(chatId, $"❌ У вас не хватает средств на балансе! ❌" +
                                            "\r\n\n" +
                                            $"💚Ваш баланс: {balance} ₽\n" +
                                            $"💛Требуется к оплате: {formattedAmountToDeduct} ₽\n\n" +
                                            $"💥Для пополнения баланса напишите /balance!", replyMarkup: inlineKeyboard);
                                    }
                                }
                                else
                                {
                                    await _botClient.SendMessage(chatId, "Произошла ошибка. 😔");
                                    hasError = true;
                                }
                            }
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Ошибка запроса: {e.Message}");
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
            CommandStateManager.DeleteCommand(chatId);

            await _botClient.SendMessage(chatId, endMsg, replyMarkup: keyBrd);
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