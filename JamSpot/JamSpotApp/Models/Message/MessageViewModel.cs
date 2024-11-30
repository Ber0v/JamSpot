namespace JamSpotApp.Models.Message
{
    public class MessageViewModel
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public string? Title { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFromAdmin { get; set; }
        public bool Pinned { get; set; }
    }
}
