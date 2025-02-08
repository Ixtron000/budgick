using Microsoft.Extensions.Configuration;

namespace TgBot.Extensions
{
    public static class AppExtensions
    {
        public static string GetConnectionString()
        {
            // Загружаем конфигурацию
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Получаем строку подключения
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Строка подключения отсутствует в файле конфигурации.");
            }

            return connectionString;
        }

        public static string GetTelegramBotToken()
        {
            // Загружаем конфигурацию
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Получаем токен
            string token = configuration["TelegramBot:Token"];

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Токен тг бота отсутствует в файле конфигурации.");
            }

            return token;
        }

        public static long GetAdminUserId()
        {
            // Загружаем конфигурацию
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Получаем строку подключения
            string userId = configuration.GetConnectionString("TelegramBot:AdminUserId");

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("UserId админа отсутствует в файле конфигурации.");
            }

            return long.Parse(userId);
        }
    }
}
