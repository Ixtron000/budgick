namespace Infrastructure.Models
{
    public class UserMessage
    {
        public long Id { get; set; }
        public long ChatId { get; set; }
        public int MessageId { get; set; }
        public string? Text { get; set; }
        public DateTime SendDate { get; set; }
    }
}
