using Autofac;
using Bussines.Factories.CommandFactory;
using DataAccess.Entities;
using Infrastructure.Models;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bussines.Factories.CallbackFactory.Callbacks
{
    public class OrdersCallbackHandler : CallbackHandlerBase
    {
        private readonly HttpClient HttpClient = new HttpClient();

        public OrdersCallbackHandler(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString) :
            base(scope, botClient, update, connectionString)
        {
        }

        public async override Task ExecuteAsync()
        {
            var keyBoard = new InlineKeyboardMarkup();

            // Начало выполнения команды myorders, инициализируем первые пять значений.
            if (CurrentStateCommand.OrdersCommand.UserOrders is null)
            {
                var orders = await GetOrders();
                CurrentStateCommand.OrdersCommand.UserOrders = orders;
                CurrentStateCommand.OrdersCommand.CurrentOrders = orders.Take(5).ToList();

                foreach (var order in CurrentStateCommand.OrdersCommand.CurrentOrders)
                {
                    keyBoard.AddNewRow(InlineKeyboardButton.WithCallbackData(order.Date.ToString(), $"myorders {order.Id}"));
                }

                if (CurrentStateCommand.OrdersCommand.UserOrders.Count > 5)
                {
                    keyBoard.AddNewRow(InlineKeyboardButton.WithCallbackData("🔙 Выйти", "main"), InlineKeyboardButton.WithCallbackData("Вперед ->", "myorders next"));
                }
                else
                {
                    keyBoard.AddNewRow(InlineKeyboardButton.WithCallbackData("🔙 Выйти", "main"));
                }

                await _botClient.SendMessage(UserId, "Выберите заказ.", replyMarkup: keyBoard);
                return;
            }

            var coordinationButton = Message.Split(" ")[1];
            // берем пять следующих доступных заказов, исключая выбранные 5 на прошлой странице
            if (coordinationButton == "next")
            {
                var orders = await GetNextFiveOrders();
                CurrentStateCommand.OrdersCommand.CurrentOrders = orders;
                foreach (var order in CurrentStateCommand.OrdersCommand.CurrentOrders)
                {
                    keyBoard.AddNewRow(InlineKeyboardButton.WithCallbackData(order.Date.ToString(), $"myorders {order.Id}"));
                }

                keyBoard.AddNewRow(
                    InlineKeyboardButton.WithCallbackData("<- Назад", "myorders prev"),
                    InlineKeyboardButton.WithCallbackData("🔙 Выйти", "main"),
                    InlineKeyboardButton.WithCallbackData("Вперед ->", "myorders next"));

                await _botClient.EditMessageText(new ChatId(UserId), _update.CallbackQuery.Message.Id, "Выберите заказ.", replyMarkup: keyBoard);
                return;
            }
            else if (coordinationButton == "prev")
            {
                // берем предыдущие пять
                var orders = await GetPrevFiveOrders();
                CurrentStateCommand.OrdersCommand.CurrentOrders = orders;
                foreach (var order in CurrentStateCommand.OrdersCommand.CurrentOrders)
                {
                    keyBoard.AddNewRow(InlineKeyboardButton.WithCallbackData(order.Date.ToString(), $"myorders {order.Id}"));
                }

                // если это первая страница
                if (orders.Contains(CurrentStateCommand.OrdersCommand.UserOrders.First()))
                {
                    keyBoard.AddNewRow(InlineKeyboardButton.WithCallbackData("🔙 Выйти", "main"), InlineKeyboardButton.WithCallbackData("Вперед ->", "myorders next"));
                }
                else
                {
                    keyBoard.AddNewRow(
                        InlineKeyboardButton.WithCallbackData("<- Назад", "myorders prev"),
                        InlineKeyboardButton.WithCallbackData("🔙 Выйти", "main"),
                        InlineKeyboardButton.WithCallbackData("Вперед ->", "myorders next"));
                }

                await _botClient.EditMessageText(new ChatId(UserId), _update.CallbackQuery.Message.Id, "Выберите заказ.", replyMarkup: keyBoard);
                return;
            }

            await GetOrderStatus();
            CommandStateManager.DeleteCommand(UserId);
        }

        private async Task<List<Order>> GetNextFiveOrders()
        {
            return CurrentStateCommand.OrdersCommand.UserOrders.SkipWhile(o => CurrentStateCommand.OrdersCommand.CurrentOrders.Any(curo => curo.Id == o.Id)).ToList();
        }

        private async Task<List<Order>> GetPrevFiveOrders()
        {
            return CurrentStateCommand.OrdersCommand.UserOrders
                .TakeWhile(o => !CurrentStateCommand.OrdersCommand.CurrentOrders.Any(curo => curo.Id == o.Id))
                .Reverse()
                .Take(5)
                .Reverse()
                .ToList();
        }

        private async Task<List<Order>> GetOrders()
        {
            var user = await _userRepository.GetUserByUserId(UserId);
            var orderEntities = await _orderRepository.FindAsync(c => c.UserId == user.Id);
            return orderEntities.OrderByDescending(o => o.Date).Select(o => MapOrderEntityToOrder(o)).ToList();
        }

        private Order MapOrderEntityToOrder(OrderEntity orderEntity)
        {
            if (orderEntity is null)
            {
                throw new ArgumentException("OrderEntity is null.");
            }

            var order = new Order()
            {
                Id = orderEntity.Id,
                Amount = orderEntity.Amount,
                Date = orderEntity.Date,
                Status = orderEntity.Status,
                UserId = orderEntity.UserId
            };

            return order;
        }

        private async Task GetOrderStatus()
        {
            try
            {
                var parts = Message.Split(' ');
                if (parts.Length < 2)
                {
                    await _botClient.SendMessage(UserId, "Вы неверно указали данные.\n");
                }
                else
                {
                    if (string.IsNullOrEmpty(parts[1]))
                    {
                        await _botClient.SendMessage(UserId, "Вы не указали идентификатор заказа.");
                    }
                    else
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

                            await _botClient.SendMessage(UserId, statusMessage, replyMarkup: inlineKeyboard);
                        }
                        else if (statusResponse.ContainsKey("error"))
                        {
                            string errorMessage = $"Ошибка при получении статуса заказа {orderId}: {statusResponse["error"]}";
                            await _botClient.SendMessage(UserId, errorMessage);
                        }
                        else
                        {
                            await _botClient.SendMessage(UserId, "Неизвестный ответ от сервера.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                await _botClient.SendMessage(UserId, $"Ошибка при получении информации о заказе: {e.Message}");
            }
        }
    }
}