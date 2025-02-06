using Infrastructure.Enums;

namespace Infrastructure.Commands
{
    public class BuyCommandModel
    {
        /// <summary>
        /// Id выбранной услуги
        /// </summary>
        public long ServiceId { get; set; }

        /// <summary>
        /// Количество услуги
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Ссылка
        /// </summary>
        public string Link { get; set; }

        public BuyCommandState State { get; set; } 
    }
}
