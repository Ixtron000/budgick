namespace DataAccess.Entities
{
    public partial class MessageEntity
    {
        public int Id { get; set; }

        public string Comand { get; set; } = null!;

        public string Text { get; set; } = null!;

        public string Type { get; set; } = null!;
    }
}
