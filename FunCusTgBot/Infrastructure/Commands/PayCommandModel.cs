using Infrastructure.Enums;

namespace Infrastructure.Commands
{
    public class PayCommandModel
    {
        public decimal Price { get; set; }
        public int PayServiceId { get; set; }
        public PayCommandState State { get; set; }
    }
}
