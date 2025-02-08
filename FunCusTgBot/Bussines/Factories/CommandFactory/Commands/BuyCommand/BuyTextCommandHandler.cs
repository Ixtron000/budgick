﻿using Bussines.Factories.CallbackFactory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bussines.Factories.CommandFactory.Commands.BuyCommand
{
    public class BuyTextCommandHandler : CommandHandlerBase
    {
        public BuyTextCommandHandler(ITelegramBotClient botClient, Update update, string connectionString) :
            base(botClient, update, connectionString)
        {
        }

        public override async Task ExecuteAsync()
        {
            var callbackHandler = CallbackHandlerFactory.GetHandler(_botClient, _update, _connectionString);
            await callbackHandler.ExecuteAsync();
        }
    }
}