namespace JamSpotApp.Data.Models
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid UserId { get; set; }
        public required User Username { get; set; }
        public string? Title { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsFromAdmin { get; set; } = false;
        public bool Pinned { get; set; } = false;
    }
}
