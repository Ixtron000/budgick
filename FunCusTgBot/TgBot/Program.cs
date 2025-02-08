using Autofac;
using Bussines.Factories.CallbackFactory;
using Bussines.Factories.CommandFactory;
using Infrastructure.Interfaces;
using Infrastructure.Models.FreeKassa;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Bcpg;
using System.Security.Cryptography;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TgBot;
using TgBot.Extensions;
using Bussines.Services;

class Program
{
    private static string ConnectionString;
    private static string Token;
    private static readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();
    private static readonly HttpClient HttpClient = new HttpClient();

    private static ILifetimeScope _scope;

    private static IUserMessageService _userMessageService;
    private static IBotClientService _botClientService;

    static async Task Main(string[] args)
    {
        // инициализируем параметры с конфига appsettings.json
        ConnectionString = AppExtensions.GetConnectionString();
        Token = AppExtensions.GetTelegramBotToken();

        // Создаём контейнер DI
        var builder = new ContainerBuilder();
        builder.RegisterModule(new InjectModule());

        var container = builder.Build();

        _scope = container.BeginLifetimeScope();
        _userMessageService = _scope.Resolve<IUserMessageService>();
        _botClientService = _scope.Resolve<IBotClientService>();

        BotClient.StartReceiving(UpdateHandler, ErrorHandler, cancellationToken: CancellationToken.Token);

        Console.WriteLine("Бот запущен.");
        Console.ReadLine();


        CancellationToken.Cancel();

    }

    private static TelegramBotClient BotClient => (TelegramBotClient)_botClientService.GetTelegramBotClient();

    private static void UpdateUserBalance(string orderId, decimal amount)
    {
        using (var connection = new MySqlConnection("Server=127.0.0.1;Database=test;User=root;Password=;"))
        {
            connection.Open();
            string query = "SELECT id, balance FROM users WHERE id = @orderId";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@orderId", orderId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int userId = reader.GetInt32("id");
                        decimal currentBalance = reader.GetDecimal("balance");
                        reader.Close();

                        decimal newBalance = currentBalance + amount;
                        query = "UPDATE users SET balance = @balance WHERE id = @userId";
                        using (var updateCommand = new MySqlCommand(query, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@balance", newBalance);
                            updateCommand.Parameters.AddWithValue("@userId", userId);
                            updateCommand.ExecuteNonQuery();
                        }

                        Console.WriteLine($"Updated balance for order {orderId}: New balance {newBalance}");
                    }
                    else
                    {
                        Console.WriteLine("Order ID not found in database.");
                    }
                }
            }
        }
    }

    //баланс
    private static async Task GetUserBalance(long chatId, ITelegramBotClient botClient, string name, long id)
    {
        using (var connection = new MySqlConnection(ConnectionString))
        {
            connection.Open();
            string query = "SELECT balance FROM users WHERE chat_id = @chatId";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@chatId", chatId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        decimal balance = reader.GetDecimal("balance");
                        var freeCassa = new FreeKassaService();

                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("оплатить", $"pay") },
                            new[] { InlineKeyboardButton.WithCallbackData("🔙 Главная", "main") }
                        });
                        await botClient.SendTextMessageAsync(
                            chatId,
                            $"🖐Здравствуйте, {name}! \n Ваш ID:  {id} \n⌛Время (МСК):  {TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"))}. \n💚Ваш баланс: {balance} руб.\n\n 🧡Для пополнения выберите сумму ниже!\n\n Для пополнения на другую сумму напишите /pay сумма",
                            replyMarkup: inlineKeyboard
                        );
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "Произошла ошибка. 😔");
                    }
                }
            }
        }
    }

    
    private static async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Произошла ошибка: {exception.Message}");
    }
    // /start
    private static async Task<string> Start(ITelegramBotClient botClient, long chatId, string name, Update update = null)
    {
        CommandStateManager.DeleteCommand(chatId);

        // Приветственное сообщение
        string welcomeMessage = "Здравствуйте! 🎉\n\n" +
            "Рады приветствовать вас в нашем сервисе. Мы здесь, чтобы помочь вам масштабировать ваш бизнес и добиться успешного продвижения в социальных сетях. 🚀\n\n" +
            "Наш бот предоставляет мощные инструменты для увеличения вашего онлайн-присутствия и повышения эффективности ваших рекламных кампаний. Мы уверены, что вы сможете достичь отличных результатов, используя наши функции. 💪\n\n" +
            "Чтобы начать, просто используйте доступные команды и исследуйте все возможности нашего сервиса. Если у вас возникнут вопросы или потребуется помощь, наша команда всегда готова прийти на помощь. 🤝\n\n" +
            "Желаем вам успешного продвижения и роста вашего бизнеса! 🌟";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Telegram 📱", "telegram"),
            InlineKeyboardButton.WithCallbackData("VK 🔵", "vk")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("TikTok 🎵", "tiktok"),
            InlineKeyboardButton.WithCallbackData("YouTube ▶️", "youtube")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Instagram 📸", "instagram"),
            InlineKeyboardButton.WithCallbackData("Rutube 🔷", "rutube")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Дзен 💚", "dzen"),
            InlineKeyboardButton.WithCallbackData("shedevrum ✨", "shedevrum")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Музыка 📣", "music"),
        }
    });

        // Сохранение информации о пользователе в базу данных
        using (var connection = new MySqlConnection(ConnectionString))
        {
            await connection.OpenAsync();

            // Check if user exists
            string checkUserQuery = "SELECT COUNT(*) FROM users WHERE chat_id = @chatId";
            using (var checkUserCommand = new MySqlCommand(checkUserQuery, connection))
            {
                checkUserCommand.Parameters.AddWithValue("@chatId", chatId);

                var userExists = Convert.ToInt32(await checkUserCommand.ExecuteScalarAsync()) > 0;

                if (!userExists)
                {
                    // Insert new user if they don't exist
                    string insertQuery = "INSERT INTO users (chat_id, name, balance) VALUES (@chatId, @name, @balance)";
                    using (var insertCommand = new MySqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@chatId", chatId);
                        insertCommand.Parameters.AddWithValue("@name", name);  // Specify the correct user name
                        insertCommand.Parameters.AddWithValue("@balance", 0);  // Initial balance

                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        // Отправка приветственного сообщения
        if (update is null)
        {
            await botClient.SendTextMessageAsync(chatId, welcomeMessage, replyMarkup: inlineKeyboard);
        }
        else
        {
            await botClient.EditMessageTextAsync(new ChatId(chatId), update.CallbackQuery.Message.Id, welcomeMessage, replyMarkup: inlineKeyboard);
        }
        
        return welcomeMessage;
    }

    // полувение под категорий
    private static async Task SendFilteredCategoriesAsync(long chatId, string messageText, string keyword, ITelegramBotClient botClient, Update update = null)
    {
        string url = "https://soc-rocket.ru/api/v2/?action=services&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP";
        var secondWordTranslations = new Dictionary<string, (string translation, string emoji)>
        {
            { "followers", ("Подписчики 📈", "followers") },
            { "views", ("Просмотры 👁️", "views") },
            { "reaction", ("Реакция ❤️", "reaction") },
            { "statistic", ("Статистика 📊", "statistic") },
            { "auto", ("Авто 🚀", "auto") },
            { "premium", ("Премиум 🌟", "premium") },
            { "other", ("Другое 🔍", "other") },
            { "friends", ("Друзья 👤", "friends") },
            { "comments", ("Комментарии ✉️", "comments") },
        };

        try
        {
            HttpResponseMessage response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            JArray jsonArray = JArray.Parse(responseBody);
            var filteredCategories = jsonArray
                .Select(service => service["category"].ToString())
                .Where(category => category.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
                .Distinct();
            var inlineKeyboardButtons = filteredCategories
                .Select(category =>
                {
                    var words = category.Split(' ');
                    if (words.Length > 1 && secondWordTranslations.TryGetValue(words[1].ToLower(), out var translation))
                    {
                        return InlineKeyboardButton.WithCallbackData($"{words[0]} {translation.translation}", category);
                    }
                    return InlineKeyboardButton.WithCallbackData(category, category);
                })
                .Select(button => new[] { button })
                .ToList();
            inlineKeyboardButtons.Add(new[] { InlineKeyboardButton.WithCallbackData("<< Главная", "main") });
            var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboardButtons);
            await botClient.EditMessageTextAsync(new ChatId(chatId), update.CallbackQuery.Message.Id, messageText, replyMarkup: inlineKeyboard);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Ошибка запроса: " + e.Message);
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при получении категорий. 😔");
        }
        catch (Newtonsoft.Json.JsonReaderException e)
        {
            Console.WriteLine("Ошибка парсинга JSON: " + e.Message);
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при парсинге ответа. 😔");
        }
    }


    //поиск айтемов по категории
    private static async Task SendFilteredItemsAsync(string category, long chatId, ITelegramBotClient botClient, Update update = null)
    {
        try
        {
            HttpResponseMessage response = await HttpClient.GetAsync("https://soc-rocket.ru/api/v2/?action=services&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JArray jsonArray = JArray.Parse(responseBody);

            var filteredItems = jsonArray
                .Where(service => service["category"]?.ToString() == category)
                .Select(service => new
                {
                    Name = service["name"]?.ToString(),
                    Service = service["service"]?.ToString()
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.Service))
                .ToList();

            var buttons = filteredItems
                .Select(item => InlineKeyboardButton.WithCallbackData(item.Name, item.Service))
                .ToList();

            buttons.Add(InlineKeyboardButton.WithCallbackData($"🔙 Назад", "main"));

            var keyboardMarkup = new InlineKeyboardMarkup(
                buttons
                    .Select(button => new[] { button })
                    .ToArray()
            );

            if (filteredItems.Any())
            {
                var msg = $"🔍 Вы выбрали категорию '{category}'. 🎯 В этом разделе собраны все доступные услуги в выбранной категории. 🌟 Ознакомьтесь с полным списком, чтобы найти именно то, что вам нужно! 📋 Если у вас возникнут вопросы или нужна помощь, мы всегда готовы помочь! 💬🔧";
                await botClient.EditMessageTextAsync(new ChatId(chatId), update.CallbackQuery.Message.Id, msg, replyMarkup: keyboardMarkup);

            }
            else
            {
                var msg = $"🚫 В категории '{category}' нет доступных услуг.";
                await botClient.EditMessageTextAsync(new ChatId(chatId), update.CallbackQuery.Message.Id, msg);
            }

        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Ошибка запроса: " + e.Message);
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при получении услуг. 😔");
        }
        catch (Newtonsoft.Json.JsonReaderException e)
        {
            Console.WriteLine("Ошибка парсинга JSON: " + e.Message);
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при парсинге ответа. 😔");
        }
    }
    // поолучение и вывод информации о заказе
    private static async Task SendServiceDetailsAsync(int serviceId, long chatId, ITelegramBotClient botClient, Update update = null)
    {
        try
        {
            HttpResponseMessage response = await HttpClient.GetAsync("https://soc-rocket.ru/api/v2/?action=services&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JArray jsonArray = JArray.Parse(responseBody);
            var service = jsonArray.FirstOrDefault(s => s["service"]?.ToString() == serviceId.ToString());

            if (service != null)
            {
                decimal rate = service["rate"].Value<decimal>();
                int price = (int)rate;
                string serviceDetails = $@"
🔸Товар №{serviceId} 🔸

🟥 Название: {service["name"]}
🟦 Цена за тысячу: {price * 2} ₽
🟧 Минимальное количество: {service["min"]}
🟩 Максимальное количество: {service["max"]}
🟨 Докрутка: {service["refill"]}
❌ Отмена: {service["cancel"]}";
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("💶 Купить", $"buy {serviceId}".ToString())
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔙 Назад", service["category"].ToString())
                    }
                });
                await botClient.EditMessageTextAsync(new ChatId(chatId), update.CallbackQuery.Message.Id, serviceDetails, replyMarkup: inlineKeyboard);

            }
            else
            {
                var msg = "⚠️ Услуга не найдена.";
                await botClient.EditMessageTextAsync(new ChatId(chatId), update.CallbackQuery.Message.Id, msg);
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Ошибка запроса: " + e.Message);
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при получении деталей услуги. 😔");
        }
        catch (Newtonsoft.Json.JsonReaderException e)
        {
            Console.WriteLine("Ошибка парсинга JSON: " + e.Message);
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при парсинге ответа. 😔");
        }
    }
    // отмена заказа
    static async Task CancelOrder(ITelegramBotClient botClient, string orderId, long chatId)
    {
        string requestUri = $"https://soc-rocket.ru/api/v2/?action=cancel&order={orderId}&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP";

        try
        {
            HttpResponseMessage response = await HttpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(responseBody);

            string cancelStatus = (string)jsonResponse["cancel"];
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔙 Назад", "main")
                    }
                });
            if (cancelStatus == "ok")
            {
                await botClient.SendTextMessageAsync(chatId: chatId, $"💚Ваш заказ №{orderId} был отменен!", replyMarkup: inlineKeyboard);
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error: {e.Message}");
        }
    }
    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        try
        {
            if (update.Message is { } message)
            {
                var commandHandler = CommandTypeHandlerFactory.GetHandler(botClient, update, ConnectionString);

                if (commandHandler is not null)
                {
                    await commandHandler.ExecuteAsync();
                    return;
                }

                if (message.Text is { } messageText)
                {
                    var chatId = message.Chat.Id;
                    if (messageText.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var connection = new MySqlConnection(ConnectionString))
                        {
                            await connection.OpenAsync();

                            bool userExists = false;

                            using (var command = new MySqlCommand("SELECT COUNT(*) FROM users WHERE chat_id = @chatId", connection))
                            {
                                command.Parameters.AddWithValue("@chatId", chatId);
                                userExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                            }

                            if (!userExists)
                            {
                                using (var command = new MySqlCommand("INSERT INTO users (chat_id, name, balance) VALUES (@chatId, @username, 0)", connection))
                                {
                                    command.Parameters.AddWithValue("@chatId", chatId);
                                    command.Parameters.AddWithValue("@username", message.From.Username);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            Console.WriteLine($"User with ChatID {chatId} and Username {message.From.Username} processed.");
                            await Start(botClient, chatId, message.From.Username);
                            return;
                        }
                    }
                    else if (messageText.StartsWith("/status"))
                    {
                        var parts = messageText.Split(' ');
                        if (parts.Length < 2)
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы неверно указали данные.\n\nПример: /status order_id");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(parts[1]))
                            {
                                await botClient.SendTextMessageAsync(chatId, "Вы не указали идентификатор заказа.");
                            }
                            else
                            {
                                try
                                {
                                    string orderId = parts[1];
                                    string statusResponseBody = await HttpClient.GetStringAsync($"https://soc-rocket.ru/api/v2/?action=status&orders={orderId}&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
                                    JObject statusResponse = JObject.Parse(statusResponseBody);

                                    if (statusResponse.ContainsKey(orderId))
                                    {

                                        var orderInfo = statusResponse[orderId];
                                        decimal rate = orderInfo["charge"].Value<decimal>();
                                        int price = (int)rate;
                                        string statusMessage =
                                                $"📝  Информация о заказе {orderId}:\n\n" +
                                                                   $"🔴 Стоимость: {price} {orderInfo["currency"]}\n" +
                                                                   $"🔹 ID: {orderInfo["service"]}\n" +
                                                                   $"🌐 Ссылка: {orderInfo["link"]}\n" +
                                                                   $"📦 Количество: {orderInfo["quantity"]}\n" +
                                                                   $"📊 Начальное количество: {orderInfo["start_count"]}\n" +
                                                                   $"📅 Дата: {orderInfo["date"]}\n" +
                                                                   $"✅ Статус: {orderInfo["status"]}\n" +
                                                                   $"📦 Остаток: {orderInfo["remains"]}";

                                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                        {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("🔙 Главная", "main")
                        }
                    });

                                        await botClient.SendTextMessageAsync(chatId, statusMessage, replyMarkup: inlineKeyboard);
                                    }
                                    else if (statusResponse.ContainsKey("error"))
                                    {
                                        string errorMessage = $"Ошибка при получении статуса заказа {orderId}: {statusResponse["error"]}";
                                        await botClient.SendTextMessageAsync(chatId, errorMessage);
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "Неизвестный ответ от сервера.");
                                    }
                                }
                                catch (HttpRequestException e)
                                {
                                    await botClient.SendTextMessageAsync(chatId, $"Ошибка запроса: {e.Message}");
                                }
                            }
                        }
                    }
                    else if (messageText == "/help")
                    {
                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithUrl("Написать","https://t.me/tekna_one")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🔙 Главная","main")
                            }
                        });
                            await botClient.SendTextMessageAsync(chatId, "⚒Столкнулись с роблемой? \n🎇 Тогда нпишите нам!🎇", replyMarkup: inlineKeyboard);
                    }
                    else if (messageText.StartsWith("/del"))
                    {
                        var parts = messageText.Split(' ');
                        if (parts.Length < 2)
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы не ввели id заказа!");
                        }
                        else
                        {
                            await CancelOrder(botClient, parts[1], chatId);
                        }
                    }
                    else if (messageText == "/balance")
                    {
                        await GetUserBalance(chatId, botClient, message.From.FirstName, message.From.Id);
                    }
                    else if (messageText.StartsWith("/pacy_add"))
                    {
                        if (message.From.Id == 1416004677)
                        {
                            var parts = messageText.Split(' ');
                            if (parts.Length < 3)
                            {
                                await botClient.SendTextMessageAsync(chatId, "Вы не ввели id заказа или данные для обновления!");
                                return;
                            }

                            string searchChatId = parts[1];
                            string newData = parts[2];

                            using (var connection = new MySqlConnection(ConnectionString))
                            {
                                await connection.OpenAsync();

                                string updateQuery = "UPDATE users SET balance = @newData WHERE chat_id = @chatId";
                                using (var command = new MySqlCommand(updateQuery, connection))
                                {
                                    command.Parameters.AddWithValue("@newData", newData);
                                    command.Parameters.AddWithValue("@chatId", searchChatId);

                                    int rowsAffected = await command.ExecuteNonQueryAsync();
                                    if (rowsAffected > 0)
                                    {
                                        string message1 = $"ID: {searchChatId}\nНовый баланс: {newData}";
                                        await botClient.SendTextMessageAsync(chatId, message1);
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "Пользователь не найден.");
                                    }
                                }
                            }
                        }
                    }
                    else if (messageText.StartsWith("/info"))
                    {
                        if (message.From.Id == 1416004677)
                        {
                            var parts = messageText.Split(' ');
                            if (parts.Length < 2)
                            {
                                await botClient.SendTextMessageAsync(chatId, "Вы не ввели ID пользователя!");
                                return;
                            }

                            string searchChatId = parts[1];

                            using (var connection = new MySqlConnection(ConnectionString))
                            {
                                await connection.OpenAsync();

                                string query = "SELECT * FROM users WHERE chat_id = @chatId";
                                using (var command = new MySqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@chatId", searchChatId);

                                    using (var reader = await command.ExecuteReaderAsync())
                                    {
                                        if (await reader.ReadAsync())
                                        {
                                            string userInfo = $"ID: {reader["chat_id"]}\n" +
                                                              $"Name: {reader["name"]}\n" +
                                                              $"Balance: {reader["balance"]}\n"
                                                              ;

                                            await botClient.SendTextMessageAsync(chatId, userInfo);
                                        }
                                        else
                                        {
                                            await botClient.SendTextMessageAsync(chatId, "Пользователь не найден.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        await Start(botClient, chatId, message.From.Username);
                    }
                }
            }
            if (update.CallbackQuery is { } callbackQuery)
            {
                var callbackHandler = CallbackHandlerFactory.GetHandler(botClient, update, ConnectionString);

                if (callbackHandler is not null)
                {
                    await callbackHandler.ExecuteAsync();
                    return;
                }

                var chatId = callbackQuery.Message.Chat.Id;
                var callbackData = callbackQuery.Data;
                if (int.TryParse(callbackData, out int serviceId))
                {
                    await SendServiceDetailsAsync(serviceId, chatId, botClient, update);
                }
                else
                {
                    switch (callbackData)
                    {
                        case "telegram":
                            await SendFilteredCategoriesAsync(chatId,
                                $"💬 **Категория: Telegram**\n\n" +
                                "✨ Мы рады предложить вам широкий выбор предложений в категории Telegram. Ознакомьтесь с нашим ассортиментом ниже и выберите то, что вам наиболее интересно! 👇\n\n" +
                                "📩 Если у вас есть вопросы или требуется помощь, не стесняйтесь обращаться к нам. Мы всегда на связи!",
                                callbackData, botClient, update);
                            break;

                        case "vk":
                            await SendFilteredCategoriesAsync(chatId,
                                $"📱 **Категория: VK**\n\n" +
                                "🎉 Добро пожаловать в категорию VK! Здесь вы найдете множество интересных предложений. Ознакомьтесь с нашим ассортиментом и выберите то, что вам больше всего нравится! 👇\n\n" +
                                "📩 Если у вас есть вопросы или требуется помощь, не стесняйтесь обращаться к нам. Мы всегда на связи!",
                                callbackData, botClient, update);
                            break;

                        case "youtube":
                            await SendFilteredCategoriesAsync(chatId,
                                $"📺 **Категория: YouTube**\n\n" +
                                "🌟 Исследуйте категорию YouTube и найдите множество увлекательных предложений. Просмотрите наш ассортимент и выберите то, что вам по душе! 👇\n\n" +
                                "📩 Если у вас есть вопросы или требуется помощь, не стесняйтесь обращаться к нам. Мы всегда на связи!",
                                callbackData, botClient, update);
                            break;

                        case "instagram":
                            await SendFilteredCategoriesAsync(chatId,
                                $"📸 **Категория: Instagram**\n\n" +
                                "📷 Добро пожаловать в категорию Instagram! Здесь вы найдете множество интересных предложений. Ознакомьтесь с нашим ассортиментом и выберите то, что вам больше всего нравится! 👇\n\n" +
                                "📩 Если у вас есть вопросы или требуется помощь, не стесняйтесь обращаться к нам. Мы всегда на связи!",
                                callbackData, botClient, update);
                            break;
                        case "main":
                            await Start(botClient, chatId, update.CallbackQuery.From.Username, update);
                            break;
                        case "Instagram likes":
                            await SendFilteredItemsAsync("Instagram likes", chatId, botClient, update);
                            break;
                        case "Instagram views":
                            await SendFilteredItemsAsync("Instagram views", chatId, botClient, update);
                            break;
                        case "Instagram followers":
                            await SendFilteredItemsAsync("Instagram followers", chatId, botClient, update);
                            break;
                        case "Instagram auto":
                            await SendFilteredItemsAsync("Instagram auto", chatId, botClient, update);
                            break;
                        case "Instagram other":
                            await SendFilteredItemsAsync("Instagram other", chatId, botClient, update);
                            break;
                        case "Instagram comments":
                            await SendFilteredItemsAsync("Instagram comments", chatId, botClient, update);
                            break;
                        case "VK likes":
                            await SendFilteredItemsAsync("VK likes", chatId, botClient, update);
                            break;
                        case "VK friends":
                            await SendFilteredItemsAsync("VK friends", chatId, botClient, update);
                            break;
                        case "VK followers":
                            await SendFilteredItemsAsync("VK followers", chatId, botClient, update);
                            break;
                        case "VK views":
                            await SendFilteredItemsAsync("VK views", chatId, botClient, update);
                            break;
                        case "VK other":
                            await SendFilteredItemsAsync("VK other", chatId, botClient, update);
                            break;
                        case "Youtube views":
                            await SendFilteredItemsAsync("Youtube views", chatId, botClient, update);
                            break;
                        case "Youtube likes":
                            await SendFilteredItemsAsync("Youtube likes", chatId, botClient, update);
                            break;
                        case "Youtube livestream":
                            await SendFilteredItemsAsync("Youtube livestream", chatId, botClient, update);
                            break;
                        case "Youtube followers":
                            await SendFilteredItemsAsync("Youtube followers", chatId, botClient, update);
                            break;
                        case "Youtube other":
                            await SendFilteredItemsAsync("Youtube other", chatId, botClient, update);
                            break;
                        case "Telegram followers":
                            await SendFilteredItemsAsync("Telegram followers", chatId, botClient, update);
                            break;
                        case "Telegram views":
                            await SendFilteredItemsAsync("Telegram views", chatId, botClient, update);
                            break;
                        case "Telegram reaction":
                            await SendFilteredItemsAsync("Telegram reaction", chatId, botClient, update);
                            break;
                        case "Telegram statistic":
                            await SendFilteredItemsAsync("Telegram statistic", chatId, botClient, update);
                            break;
                        case "Telegram auto":
                            await SendFilteredItemsAsync("Telegram auto", chatId, botClient, update);
                            break;
                        case "Telegram premium":
                            await SendFilteredItemsAsync("Telegram premium", chatId, botClient, update);
                            break;
                        case "Telegram other":
                            await SendFilteredItemsAsync("Telegram other", chatId, botClient, update);
                            break;
                        case "tiktok":
                            await SendFilteredItemsAsync("tiktok", chatId, botClient, update);
                            break;
                        case "rutube":
                            await SendFilteredItemsAsync("rutube", chatId, botClient, update);
                            break;
                        case "dzen":
                            await SendFilteredItemsAsync("dzen", chatId, botClient, update);
                            break;
                        case "shedevrum":
                            await SendFilteredItemsAsync("shedevrum", chatId, botClient, update);
                            break;
                        case "music":
                            await SendFilteredItemsAsync("music", chatId, botClient, update);
                            break;
                        default:
                            if (callbackData.StartsWith("check"))
                            {
                                var parts = callbackData.Split(' ');
                                if (parts.Length < 2)
                                {
                                    return;
                                }
                                var orderId = parts[1];
                                var freeCassa = new FreeKassaService();
                                var orderResponse = await freeCassa.GetOrderAsync(orderId);
                                if (orderResponse != null && orderResponse.ContainsKey("orders"))
                                {
                                    var orders = JsonConvert.SerializeObject(orderResponse["orders"]);
                                    var ordersArray = JArray.Parse(orderResponse["orders"].ToString());
                                    foreach (var order in ordersArray)
                                    {
                                        var statusMessage = "Платеж не оплачен❌"; // Default message for unsuccessful payment
                                        if ((int)order["status"] == 1)
                                        {
                                            using (var connection = new MySqlConnection(ConnectionString))
                                            {
                                                await connection.OpenAsync();

                                                // Сначала получаем текущий баланс пользователя
                                                string selectQuery = "SELECT balance FROM users WHERE chat_id = @chatId";
                                                decimal currentBalance = 0;
                                                using (var selectCommand = new MySqlCommand(selectQuery, connection))
                                                {
                                                    selectCommand.Parameters.AddWithValue("@chatId", callbackQuery.From.Id);
                                                    var result = await selectCommand.ExecuteScalarAsync();
                                                    if (result != null)
                                                    {
                                                        currentBalance = Convert.ToDecimal(result);
                                                    }
                                                }

                                                // Плюсуем новый платеж к текущему балансу
                                                decimal newBalance = currentBalance + (decimal)order["amount"];

                                                // Обновляем баланс в базе данных
                                                string updateQuery = "UPDATE users SET balance = @newBalance WHERE chat_id = @chatId";
                                                using (var updateCommand = new MySqlCommand(updateQuery, connection))
                                                {
                                                    updateCommand.Parameters.AddWithValue("@newBalance", newBalance);
                                                    updateCommand.Parameters.AddWithValue("@chatId", callbackQuery.From.Id);
                                                    await updateCommand.ExecuteNonQueryAsync();
                                                }
                                            }
                                            statusMessage = "Платеж был зачислен💚"; // Message for successful payment
                                        }

                                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                        {
                                            new[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главная", "main")
                                            }
                                        });
                                        await botClient.SendTextMessageAsync(
                                            chatId,
                                            statusMessage,
                                            replyMarkup: inlineKeyboard
                                        );
                                    }
                                }
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId, "⚠️ Неизвестная команда.");
                            }
                            break;

                    }
                }
            }
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 403)
        {
            Console.WriteLine("Произошла ошибка: Forbidden: bot was blocked by the user");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }
}
//dotnet publish -c Release -r ubuntu.22.04-x64