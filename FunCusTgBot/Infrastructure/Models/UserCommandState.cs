using Infrastructure.Commands;
using Infrastructure.Enums;

namespace Infrastructure.Models
{
    public class UserCommandState
    {
        public UserCommandState(long userId, string command, BuyCommandModel buyCommandModel)
        {
            UserId = userId;
            BuyCommand = buyCommandModel;
            Command = command;
        }

        public long UserId { get; }

        public string Command { get; }

        public BuyCommandModel BuyCommand { get; }

        public static UserCommandState Create(long userId, string command, BuyCommandModel buyCommandModel = null)
        {
            return new UserCommandState(userId, command, buyCommandModel);
        }
    }
}
