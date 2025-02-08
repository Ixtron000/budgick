using Infrastructure.Enums;

namespace Infrastructure.Commands
{
    public class CheckCommandModel
    {
        public long OrderId { get; set; }
        public CheckCommandState State { get; set; }
    }
}
