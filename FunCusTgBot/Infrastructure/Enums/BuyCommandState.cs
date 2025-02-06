namespace Infrastructure.Enums
{
    /// <summary>
    /// Статусная машина для команды /buy
    /// </summary>
    public enum BuyCommandState
    {
        /// <summary>
        /// Статус команды отсутсвует
        /// </summary>
        None,

        /// <summary>
        /// Статус выбора id услуги
        /// </summary>
        ChooseServiceId,

        /// <summary>
        /// Статус выбора количество услуги
        /// </summary>
        ChooseCount,

        /// <summary>
        /// Статус ожидания отправки ссылки на предоставление услуги
        /// </summary>
        SendLink
    }
}
