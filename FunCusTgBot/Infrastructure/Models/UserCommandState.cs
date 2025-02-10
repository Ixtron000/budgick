using Infrastructure.Commands;

namespace Infrastructure.Models
{
    public class UserCommandState
    {
        private UserCommandState(long userId, string command, 
            BuyCommandModel buyCommandModel = null, 
            PayCommandModel payCommand = null,
            CheckCommandModel checkCommand = null,
            OrdersCommandModel ordersCommand = null)
        {
            UserId = userId;
            Command = command;
            BuyCommand = buyCommandModel;
            PayCommand = payCommand;
            CheckCommand = checkCommand;
            OrdersCommand = ordersCommand;
        }

        public long UserId { get; }

        public string Command { get; }

        public BuyCommandModel BuyCommand { get; }
        
        public PayCommandModel PayCommand { get; }

        public CheckCommandModel CheckCommand { get; }

        public OrdersCommandModel OrdersCommand { get; }

        public static UserCommandState Create(long userId, string command)
        {
            if (command == "buy")
            {
                var commandModel = new BuyCommandModel();
                return new UserCommandState(userId, command, buyCommandModel: commandModel);
            }

            if (command == "pay")
            {
                var commandModel = new PayCommandModel();
                return new UserCommandState(userId, command, payCommand: commandModel);
            }

            if (command == "check")
            {
                var commandModel = new CheckCommandModel();
                return new UserCommandState(userId, command, checkCommand: commandModel);
            }

            if (command == "myorders")
            {
                var commandModel = new OrdersCommandModel();
                return new UserCommandState(userId, command, ordersCommand: commandModel);
            }

            throw new ArgumentException("Ошибка создания команды");
        }
    }
}
