namespace Infrastructure.Models
{
    public class Order
    {
        public int Id { get; set; }

        // это именно колонка Id в таблице users, но не charId
        public int UserId { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; } = null!;

        public DateTime Date { get; set; }
    }
}
