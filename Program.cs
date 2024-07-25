using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using OfficeOpenXml;
using Telegram.Bot.Exceptions;

class Program
{
    private static readonly string Token = "7197293618:AAEdjKHiF2mFo5MaM7bHLK9vuumdEsWisgQ";
    private static readonly TelegramBotClient BotClient = new TelegramBotClient(Token);
    private static readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();
    private static readonly HttpClient HttpClient = new HttpClient(); 

    static async Task Main(string[] args)
    {
        BotClient.StartReceiving(UpdateHandler, ErrorHandler, cancellationToken: CancellationToken.Token);

        Console.WriteLine("Бот запущен.");
        Console.ReadLine();

        CancellationToken.Cancel();
    }

    private static async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Произошла ошибка: {exception.Message}");
    }
    // /start
    private static async Task Start(ITelegramBotClient botClient, long chatId)
    {
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


        await botClient.SendTextMessageAsync(chatId, welcomeMessage, replyMarkup: inlineKeyboard);
    }
    // полувение под категорий
    private static async Task SendFilteredCategoriesAsync(long chatId, string messageText, string keyword, ITelegramBotClient botClient)
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
            await botClient.SendTextMessageAsync(chatId, messageText, replyMarkup: inlineKeyboard);
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
    //баланс
    private static async Task GetUserBalance(long chatId, ITelegramBotClient botClient, string name, long id )
    {
        string filePath = "users.xlsx";
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine("Файл не найден.");
            return;
        }

        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var worksheet = package.Workbook.Worksheets["Users"];
            var rowCount = worksheet.Dimension?.Rows;

            if (rowCount.HasValue)
            {
                bool userFound = false;

                for (int row = 2; row <= rowCount.Value; row++)
                {
                    if (worksheet.Cells[row, 1].Value.ToString() == chatId.ToString())
                    {
                        var balance = worksheet.Cells[row, 3].Value;
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
                        await botClient.SendTextMessageAsync(
                            chatId,
                            $"🖐Здравствуйте, {name}! \n Ваш ID:  {id} \n⌛Время (МСК):  {TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"))}. \n💚Ваш баланс: {balance} руб.\n\n 🧡Для пополнения напишите нам!",
                            replyMarkup: inlineKeyboard
                        );
                        userFound = true;
                        break;
                    }
                }
                if (!userFound)
                {
                    await botClient.SendTextMessageAsync(chatId, "Произошла ошибка. 😔");
                }
            }
            else
            {
                Console.WriteLine("В файле нет данных.");
            }
        }
    }
    //поиск айтемов по категории
    private static async Task SendFilteredItemsAsync(string category, long chatId, ITelegramBotClient botClient)
    {
        try
        {
            HttpResponseMessage response = await HttpClient.GetAsync("https://soc-rocket.ru/api/v2/?action=services&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JArray jsonArray = JArray.Parse(responseBody);

            string firstWord = category.Split(' ').FirstOrDefault();

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

            string modifiedFirstWord = char.ToLower(firstWord[0]) + firstWord.Substring(1);
            buttons.Add(InlineKeyboardButton.WithCallbackData($"🔙 Назад", $"{modifiedFirstWord}"));


            var keyboardMarkup = new InlineKeyboardMarkup(
                buttons
                    .Select(button => new[] { button }) 
                    .ToArray()
            );

            if (filteredItems.Any())
            {
                await botClient.SendTextMessageAsync(chatId, $"🔍 Вы выбрали категорию '{category}'. 🎯 В этом разделе собраны все доступные услуги в выбранной категории. 🌟 Ознакомьтесь с полным списком, чтобы найти именно то, что вам нужно! 📋 Если у вас возникнут вопросы или нужна помощь, мы всегда готовы помочь! 💬🔧", replyMarkup: keyboardMarkup);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, $"🚫 В категории '{category}' нет доступных услуг.");
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
    private static async Task SendServiceDetailsAsync(int serviceId, long chatId, ITelegramBotClient botClient)
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
❌ Отмена: {service["cancel"]}

💶Чтобы купить, напишите команду 🛒:
/buy {serviceId} количество ссылка";
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔙 Назад", service["category"].ToString())
                    }
                });
                await botClient.SendTextMessageAsync(chatId, serviceDetails, replyMarkup: inlineKeyboard);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "⚠️ Услуга не найдена.");
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
                if (message.Text is { } messageText)
                {
                    var chatId = message.Chat.Id;
                    if (messageText.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                    {
                        string filePath = "users.xlsx";
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                        if (!System.IO.File.Exists(filePath))
                        {
                            using (var package = new ExcelPackage())
                            {
                                var worksheet = package.Workbook.Worksheets.Add("Users");
                                worksheet.Cells[1, 1].Value = "ChatID";
                                worksheet.Cells[1, 2].Value = "Username";
                                worksheet.Cells[1, 3].Value = "Balance";
                                package.SaveAs(new FileInfo(filePath));
                            }
                        }

                        bool userExists = false;

                        using (var package = new ExcelPackage(new FileInfo(filePath)))
                        {
                            var worksheet = package.Workbook.Worksheets["Users"];
                            var rowCount = worksheet.Dimension?.Rows;

                            if (rowCount.HasValue)
                            {
                                for (int row = 2; row <= rowCount.Value; row++)
                                {
                                    if (worksheet.Cells[row, 1].Value.ToString() == chatId.ToString())
                                    {
                                        userExists = true;
                                        break;
                                    }
                                }
                            }

                            if (!userExists)
                            {
                                var newRow = rowCount.HasValue ? rowCount.Value + 1 : 2;
                                worksheet.Cells[newRow, 1].Value = chatId;
                                worksheet.Cells[newRow, 2].Value = message.From.Username;
                                worksheet.Cells[newRow, 3].Value = 0;
                                package.Save();
                            }
                        }

                        Console.WriteLine($"User with ChatID {chatId} and Username {message.From.Username} processed.");
                        await Start(botClient, chatId);
                        return;
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
                    else if (messageText.StartsWith("/buy"))
                    {
                        var parts = messageText.Split(' ');
                        if (parts.Length < 4)
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы неверно указали данные.\n\nПример: /buy 330 количество ссылка");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(parts[1]))
                            {
                                await botClient.SendTextMessageAsync(chatId, "Вы не указали id услуги.");
                            }
                            if (string.IsNullOrEmpty(parts[2]))
                            {
                                await botClient.SendTextMessageAsync(chatId, "Вы не указали количество.");
                            }
                            if (string.IsNullOrEmpty(parts[3]))
                            {
                                await botClient.SendTextMessageAsync(chatId, "Вы не указали ссылку.");
                            }
                            if (!string.IsNullOrEmpty(parts[1]) && !string.IsNullOrEmpty(parts[2]) && !string.IsNullOrEmpty(parts[3]))
                            {
                                try
                                {
                                    HttpResponseMessage response = await HttpClient.GetAsync("https://soc-rocket.ru/api/v2/?action=services&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
                                    response.EnsureSuccessStatusCode();

                                    string responseBody1 = await response.Content.ReadAsStringAsync();
                                    JArray jsonArray = JArray.Parse(responseBody1);
                                    var service = jsonArray.FirstOrDefault(s => s["service"]?.ToString() == parts[1]);

                                    if (service != null)
                                    {
                                        decimal rate = service["rate"].Value<decimal>();
                                        decimal price = rate * 2;
                                        Console.WriteLine($"Rate: {rate}, Price: {price}");

                                        string filePath = "users.xlsx";
                                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                                        if (!System.IO.File.Exists(filePath))
                                        {
                                            Console.WriteLine("Файл не найден.");
                                            return;
                                        }

                                        using (var package = new ExcelPackage(new FileInfo(filePath)))
                                        {
                                            var worksheet = package.Workbook.Worksheets["Users"];
                                            var rowCount = worksheet.Dimension?.Rows;

                                            if (rowCount.HasValue)
                                            {
                                                bool userFound = false;

                                                for (int row = 2; row <= rowCount.Value; row++)
                                                {
                                                    if (worksheet.Cells[row, 1].Value.ToString() == chatId.ToString())
                                                    {
                                                        var balanceValue = worksheet.Cells[row, 3].Value;
                                                        if (balanceValue == null || !decimal.TryParse(balanceValue.ToString(), out decimal balance))
                                                        {
                                                            await botClient.SendTextMessageAsync(chatId, "Ошибка при чтении баланса пользователя.");
                                                            return;
                                                        }
                                                        if (int.TryParse(parts[2], out int partsValue))
                                                        {
                                                            decimal amountToDeduct = (price / 1000m) * partsValue;
                                                            string formattedAmountToDeduct = amountToDeduct.ToString("0.0");
                                                            Console.WriteLine($"Balance: {balance}, Amount to Deduct: {amountToDeduct}");
                                                            if (balance >= amountToDeduct)
                                                            {
                                                                // Выполнение заказа
                                                                string responseBody = await HttpClient.GetStringAsync($"https://soc-rocket.ru/api/v2/?action=add&service={parts[1]}&link={parts[3]}&quantity={parts[2]}&key=bXmgSXp94cHDrOmaNbhNtGtlEoSmniiP");
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

                                                                        await botClient.SendTextMessageAsync(chatId, statusMessage, replyMarkup: inlineKeyboard);
                                                                        worksheet.Cells[row, 3].Value = balance - amountToDeduct;
                                                                        package.Save();
                                                                    }
                                                                    else if (statusResponse.ContainsKey("error"))
                                                                    {
                                                                        string errorMessage = $"Ошибка при получении статуса заказа {orderId}: {statusResponse["error"]}";
                                                                        await botClient.SendTextMessageAsync(chatId, errorMessage);
                                                                    }
                                                                }
                                                                else if (jsonResponse.ContainsKey("error"))
                                                                {
                                                                    await botClient.SendTextMessageAsync(chatId, $"Ошибка: {jsonResponse["error"]}");
                                                                }
                                                                else
                                                                {
                                                                    Console.WriteLine("Неизвестный ответ от сервера");
                                                                }

                                                                userFound = true;
                                                                break;
                                                            }
                                                            else
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
                                                                await botClient.SendTextMessageAsync(chatId, $"❌ У вас не хватает средств на балансе! ❌" +
                                                                    "\r\n\n" +
                                                                    $"💚Ваш баланс: {balance} ₽\n" +
                                                                    $"💛Требуется к оплате: {formattedAmountToDeduct} ₽\n\n" +
                                                                    $"💥Для пополнения баланса напишите нам!", replyMarkup: inlineKeyboard);
                                                            }
                                                        }
                                                    }
                                                }
                                                if (!userFound)
                                                {
                                                    await botClient.SendTextMessageAsync(chatId, "Произошла ошибка. 😔");
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("В файле нет данных.");
                                            }
                                        }
                                    }
                                }
                                catch (HttpRequestException e)
                                {
                                    Console.WriteLine($"Ошибка запроса: {e.Message}");
                                }
                            }
                        }
                    }
                    else if (messageText == "/balance")
                    {
                        await GetUserBalance(chatId, botClient, message.From.FirstName, message.From.Id);
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
                    else if (messageText.StartsWith("/pay"))
                    {
                        if (message.From.Id == 1416004677)
                        {
                            string filePath = "users.xlsx";
                            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                            if (!System.IO.File.Exists(filePath))
                            {
                                Console.WriteLine("Файл не найден.");
                                return;
                            }

                            var parts = messageText.Split(' ');
                            if (parts.Length < 3)
                            {
                                await botClient.SendTextMessageAsync(chatId, "Вы не ввели id заказа или данные для обновления!");
                                return;
                            }

                            string searchChatId = parts[1];
                            string newData = parts[2];

                            using (var package = new ExcelPackage(new FileInfo(filePath)))
                            {
                                var worksheet = package.Workbook.Worksheets["Users"];
                                var rowCount = worksheet.Dimension?.Rows;

                                if (rowCount.HasValue)
                                {
                                    bool userFound = false;

                                    for (int row = 2; row <= rowCount.Value; row++)
                                    {
                                        if (worksheet.Cells[row, 1].Value.ToString() == searchChatId)
                                        {
                                            // Assuming the column to update is column 3 (you can change this to the correct column index)
                                            worksheet.Cells[row, 3].Value = newData;
                                            package.Save();

                                            string message1 = $"ID: {searchChatId}\nНовый баланс: {newData}";
                                            await botClient.SendTextMessageAsync(chatId, message1);
                                            userFound = true;
                                            break;
                                        }
                                    }
                                    if (!userFound)
                                    {
                                        await botClient.SendTextMessageAsync(chatId, "Пользователь не найден.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("В файле нет данных.");
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
                    else
                    {
                        await Start(botClient, chatId);
                    }

                }
            }

            if (update.CallbackQuery is { } callbackQuery)
            {
                var chatId = callbackQuery.Message.Chat.Id;
                var callbackData = callbackQuery.Data;
                if (int.TryParse(callbackData, out int serviceId))
                {
                    await SendServiceDetailsAsync(serviceId, chatId, botClient);
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
                                callbackData, botClient);
                            break;

                        case "vk":
                            await SendFilteredCategoriesAsync(chatId,
                                $"📱 **Категория: VK**\n\n" +
                                "🎉 Добро пожаловать в категорию VK! Здесь вы найдете множество интересных предложений. Ознакомьтесь с нашим ассортиментом и выберите то, что вам больше всего нравится! 👇\n\n" +
                                "📩 Если у вас есть вопросы или требуется помощь, не стесняйтесь обращаться к нам. Мы всегда на связи!",
                                callbackData, botClient);
                            break;

                        case "youtube":
                            await SendFilteredCategoriesAsync(chatId,
                                $"📺 **Категория: YouTube**\n\n" +
                                "🌟 Исследуйте категорию YouTube и найдите множество увлекательных предложений. Просмотрите наш ассортимент и выберите то, что вам по душе! 👇\n\n" +
                                "📩 Если у вас есть вопросы или требуется помощь, не стесняйтесь обращаться к нам. Мы всегда на связи!",
                                callbackData, botClient);
                            break;

                        case "instagram":
                            await SendFilteredCategoriesAsync(chatId,
                                $"📸 **Категория: Instagram**\n\n" +
                                "📷 Добро пожаловать в категорию Instagram! Здесь вы найдете множество интересных предложений. Ознакомьтесь с нашим ассортиментом и выберите то, что вам больше всего нравится! 👇\n\n" +
                                "📩 Если у вас есть вопросы или требуется помощь, не стесняйтесь обращаться к нам. Мы всегда на связи!",
                                callbackData, botClient);
                            break;
                        case "main":
                            await Start(botClient, chatId);
                            break;
                        case "Instagram likes":
                            await SendFilteredItemsAsync("Instagram likes", chatId, botClient);
                            break;
                        case "Instagram views":
                            await SendFilteredItemsAsync("Instagram views", chatId, botClient);
                            break;
                        case "Instagram followers":
                            await SendFilteredItemsAsync("Instagram followers", chatId, botClient);
                            break;
                        case "Instagram auto":
                            await SendFilteredItemsAsync("Instagram auto", chatId, botClient);
                            break;
                        case "Instagram other":
                            await SendFilteredItemsAsync("Instagram other", chatId, botClient);
                            break;
                        case "Instagram comments":
                            await SendFilteredItemsAsync("Instagram comments", chatId, botClient);
                            break;
                        case "VK likes":
                            await SendFilteredItemsAsync("VK likes", chatId, botClient);
                            break;
                        case "VK friends":
                            await SendFilteredItemsAsync("VK friends", chatId, botClient);
                            break;
                        case "VK followers":
                            await SendFilteredItemsAsync("VK followers", chatId, botClient);
                            break;
                        case "VK views":
                            await SendFilteredItemsAsync("VK views", chatId, botClient);
                            break;
                        case "VK other":
                            await SendFilteredItemsAsync("VK other", chatId, botClient);
                            break;
                        case "Youtube views":
                            await SendFilteredItemsAsync("Youtube views", chatId, botClient);
                            break;
                        case "Youtube likes":
                            await SendFilteredItemsAsync("Youtube likes", chatId, botClient);
                            break;
                        case "Youtube livestream":
                            await SendFilteredItemsAsync("Youtube livestream", chatId, botClient);
                            break;
                        case "Youtube followers":
                            await SendFilteredItemsAsync("Youtube followers", chatId, botClient);
                            break;
                        case "Youtube other":
                            await SendFilteredItemsAsync("Youtube other", chatId, botClient);
                            break;
                        case "Telegram followers":
                            await SendFilteredItemsAsync("Telegram followers", chatId, botClient);
                            break;
                        case "Telegram views":
                            await SendFilteredItemsAsync("Telegram views", chatId, botClient);
                            break;
                        case "Telegram reaction":
                            await SendFilteredItemsAsync("Telegram reaction", chatId, botClient);
                            break;
                        case "Telegram statistic":
                            await SendFilteredItemsAsync("Telegram statistic", chatId, botClient);
                            break;
                        case "Telegram auto":
                            await SendFilteredItemsAsync("Telegram auto", chatId, botClient);
                            break;
                        case "Telegram premium":
                            await SendFilteredItemsAsync("Telegram premium", chatId, botClient);
                            break;
                        case "Telegram other":
                            await SendFilteredItemsAsync("Telegram other", chatId, botClient);
                            break;
                        case "tiktok":
                            await SendFilteredItemsAsync("tiktok", chatId, botClient);
                            break;
                        case "rutube":
                            await SendFilteredItemsAsync("rutube", chatId, botClient);
                            break;
                        case "dzen":
                            await SendFilteredItemsAsync("dzen", chatId, botClient);
                            break;
                        case "shedevrum":
                            await SendFilteredItemsAsync("shedevrum", chatId, botClient);
                            break;
                        case "music":
                            await SendFilteredItemsAsync("music", chatId, botClient);
                            break;
                        default:
                            await botClient.SendTextMessageAsync(chatId, "⚠️ Неизвестная команда.");
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
