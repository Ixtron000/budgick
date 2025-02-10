using Autofac;
using Bussines.Factories.CommandFactory;
using Infrastructure.Enums;
using Infrastructure.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bussines.Factories.CallbackFactory.Callbacks
{
    public class CheckCallbackHandler : CallbackHandlerBase
    {
        public CheckCallbackHandler(ILifetimeScope scope, ITelegramBotClient botClient, Update update, string connectionString) :
            base(scope, botClient, update, connectionString)
        {
        }

        public async override Task ExecuteAsync()
        {
            if (CurrentStateCommand.CheckCommand.State is CheckCommandState.None)
            {
                var orderIdStr = Message.Split(' ')[1];
                if (long.TryParse(orderIdStr, out long orderId))
                {
                    CurrentStateCommand.CheckCommand.OrderId = orderId;
                    CurrentStateCommand.CheckCommand.State = CheckCommandState.ChooseOrder;
                    CommandStateManager.AddCommand(CurrentStateCommand);
                }
                else
                {
                    await _botClient.SendMessage(UserId, "Не удалось получить id заказа для проверки.");
                    CommandStateManager.DeleteCommand(UserId);
                    return;
                }
            }

            if (CurrentStateCommand.CheckCommand.State is CheckCommandState.ChooseOrder)
            {
                var orderResponse = await _freeKassaService.GetOrderAsync(CurrentStateCommand.CheckCommand.OrderId);
                if (orderResponse != null && orderResponse.ContainsKey("orders"))
                {
                    var orders = JsonConvert.SerializeObject(orderResponse["orders"]);
                    var ordersArray = JArray.Parse(orderResponse["orders"].ToString());
                    foreach (var order in ordersArray)
                    {
                        var orderModel = new Order()
                        {
                            Id = (int)order["fk_order_id"],
                            Amount = (decimal)order["amount"],
                            Date = (DateTime)order["date"],
                            Status = (string)order["status"],
                        };

                        await _orderService.CreateOrUpdateStatusOrder(UserId, orderModel);

                        var statusMessage = "Платеж не оплачен❌";
                        if (orderModel.Status == "1")
                        {
                            var user = await _userRepository.GetByIdAsync(UserId);
                            user.Balance = user.Balance + orderModel.Amount;
                            await _userRepository.UpdateAsync(user);

                            statusMessage = "Платеж был зачислен💚";
                        }

                        var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Главная", "main") } });
                        await _botClient.SendMessage(UserId, statusMessage, replyMarkup: inlineKeyboard);
                    }
                }
            }

            CommandStateManager.DeleteCommand(UserId);
        }
    }
}
