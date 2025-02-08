namespace DataAccess.Entities
{
    public partial class UserEntity
    {
        public int Id { get; set; }

        public long ChatId { get; set; }

        public string Name { get; set; } = null!;

        public decimal Balance { get; set; }

        public bool? Admin { get; set; }

        public virtual ICollection<OrderEntity> Orders { get; set; } = new List<OrderEntity>();
    }
}
