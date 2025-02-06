using Infrastructure.Models;

namespace Bussines.Factories.CommandFactory
{
    public static class CommandStateManager
    {
        private static readonly Dictionary<long, UserCommandState> _userCommandStates = new();

        public static void AddCommand(UserCommandState userCommandState)
        {
            try
            {
                _userCommandStates[userCommandState.UserId] = userCommandState;
            }
            catch 
            {
                throw new ArgumentException("Ошибка добавления команды пользователя в машину состояний.");
            }
        }

        public static UserCommandState GetCommand(long userId)
        {
            if (_userCommandStates.TryGetValue(userId, out var command))
            {
                return command;
            }

            throw new ArgumentException("Ошибка получения команды пользователя в машину состояний.");
        }

        /// <summary>
        /// Существует ли пользователь в состоянии выполнения команды
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool IsExistsState(long userId)
        {
            if (_userCommandStates.TryGetValue(userId, out var command))
            {
                return true;
            }

            return false;
        }

        public static void DeleteCommand(long userId)
        {
            _userCommandStates.Remove(userId);
        }
    }
}
