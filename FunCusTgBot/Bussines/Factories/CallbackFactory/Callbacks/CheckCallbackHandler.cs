using Autofac;
using Bussines.Services;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MySql.Data.MySqlClient;
using Bussines.Factories.CommandFactory;
using Infrastructure.Enums;
using Mysqlx.Resultset;

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
                        var statusMessage = "Платеж не оплачен❌";
                        if ((int)order["status"] == 1)
                        {
                            var user = await _userRepository.GetByIdAsync(UserId);
                            user.Balance = user.Balance + (decimal)order["amount"];
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
