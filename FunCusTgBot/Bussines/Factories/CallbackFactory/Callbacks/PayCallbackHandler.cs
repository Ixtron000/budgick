using Autofac;
using Bussines.Factories.CommandFactory;
using Infrastructure.Enums;
using Infrastructure.Models.FreeKassa;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bussines.Factories.CallbackFactory.Callbacks
{
    public class PayCallbackHandler : CallbackHandlerBase
    {
        private readonly HttpClient HttpClient = new HttpClient();

        public PayCallbackHandler(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString) :
            base(scope, botClient, update, connectionString)
        {
        }

        public async override Task ExecuteAsync()
        {
            try
            {
                if (CurrentStateCommand.PayCommand.State is PayCommandState.None)
                {
                    await _botClient.SendMessage(UserId, "Введите сумму");
                    CurrentStateCommand.PayCommand.State = PayCommandState.Price;
                    return;
                }

                if (CurrentStateCommand.PayCommand.State is PayCommandState.Price)
                {
                    var msg = "Формирование способа оплаты.";
                    
                    var priceStr = Message;
                    if (decimal.TryParse(priceStr, out decimal price))
                    {
                        CurrentStateCommand.PayCommand.Price = price;
                        CurrentStateCommand.PayCommand.State = PayCommandState.ChoosePayService;
                        CommandStateManager.AddCommand(CurrentStateCommand);
                    }
                    else
                    {
                        msg = "Сумма указывается только в цифрах.";
                        await _botClient.SendMessage(UserId, msg);
                        return;
                    }

                    await _botClient.SendMessage(UserId, msg);
                }

                
                if (CurrentStateCommand.PayCommand.State is PayCommandState.ChoosePayService)
                {
                    var availableCurrencies = await GetAvailableCurrencies(CurrentStateCommand.PayCommand.Price);

                    if (availableCurrencies.Count > 1)
                    {
                        var inlineKeyBoard = new InlineKeyboardMarkup();
                        foreach (var currency in availableCurrencies)
                        {
                            inlineKeyBoard.AddNewRow(InlineKeyboardButton.WithCallbackData(currency.Name, $"pay {currency.Id}"));
                        }

                        inlineKeyBoard.AddNewRow(InlineKeyboardButton.WithCallbackData("🔙 Назад", "main"));

                        await _botClient.SendMessage(UserId,"Выберите способ оплаты.", replyMarkup: inlineKeyBoard);
                        CurrentStateCommand.PayCommand.State = PayCommandState.CreateOrder;
                        return;
                    }
                    else
                    {
                        throw new ArgumentException("Доступные способы пополнения отсутствуют.");
                    }
                }

                if (CurrentStateCommand.PayCommand.State is PayCommandState.CreateOrder)
                {
                    var payServiceIdStr = Message.Split(" ")[1];
                    if (int.TryParse(payServiceIdStr, out int payServiceId))
                    {
                        CurrentStateCommand.PayCommand.PayServiceId = payServiceId;
                        CommandStateManager.AddCommand(CurrentStateCommand);
                    }
                    else
                    {
                        await _botClient.SendMessage(UserId, "Не выбран способ оплаты.");
                        return;
                    }

                    try
                    {
                        await CreateOrder(CurrentStateCommand.PayCommand.Price, CurrentStateCommand.PayCommand.PayServiceId);
                    }
                    catch (Exception ex)
                    {
                        await _botClient.SendMessage(UserId, "Данный способ оплаты в данный момент недоступен.");
                        return;
                    }
                }
                else
                {
                    throw new ArgumentException("Ошибка формирования способа оплаты.");
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendMessage(UserId, ex.Message);
                CommandStateManager.DeleteCommand(UserId);
                return;
            }

            CommandStateManager.DeleteCommand(UserId);
        }

        private async Task<List<Currency>> GetAvailableCurrencies(decimal price)
        {
            var currencies = await _freeKassaService.GetCurrencies();
            var availvablePaySystem = await _freeKassaService.GetAvailableCurrencies(currencies.Currencies);
            return availvablePaySystem.Where(c => c.Limits.Min <= price && price <= c.Limits.Max).ToList();
        }

        private async Task CreateOrder(decimal price, int paySystemId)
        {
            var response = await _freeKassaService.CreateLinkForPayAsync(UserId, (double)price, paySystemId);

            var orderId = long.Parse(response["orderId"].ToString());
            var orderResponse = await _freeKassaService.GetOrderAsync(orderId);
            if (orderResponse != null && orderResponse.ContainsKey("orders"))
            {
                var orders = JsonConvert.SerializeObject(orderResponse["orders"]);
                var ordersArray = JArray.Parse(orderResponse["orders"].ToString());
                foreach (var order in ordersArray)
                {
                    var status = GetOrderStatus((int)order["status"]);
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithUrl($"Оплатить {price} руб.", response["location"].ToString())
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✨ Проверить ✨", "check " + order["fk_order_id"])
                        }
                    });

                    await _botClient.SendMessage(
                        UserId,
                        $"✅Для пополнения вашего баланса на {price} рублей, перейдите по следующей ссылке.\n\n 🔴Информация о платеже №{order["fk_order_id"]}\r\n  💰Сумма: {order["amount"]} \n  ⏳Дата: {order["date"]} \n  🔵Статус платежа: {status}\n\n 🔴После опалты нажмите кнопку провертить!",
                        replyMarkup: inlineKeyboard
                    );
                    Console.WriteLine($"Order ID: {order["fk_order_id"]}, Status: {order["status"]}");
                }
            }
        }

        private string GetOrderStatus(int orderState)
        {
            if (orderState == 0)
            {
                return "Новый";
            }
            else if (orderState == 1)
            {
                return "Оплачен";
            }
            else if (orderState == 8)
            {
                return "Ошибка";
            }
            else if (orderState == 9)
            {
                return "Отмена";
            }

            return string.Empty;
        }
    }
}
