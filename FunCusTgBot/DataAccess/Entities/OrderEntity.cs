namespace DataAccess.Entities
{
    public partial class OrderEntity
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; } = null!;

        public DateTime Date { get; set; }

        public virtual UserEntity User { get; set; } = null!;
    }
}
