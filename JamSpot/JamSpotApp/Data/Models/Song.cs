namespace JamSpotApp.Data.Models
{
    public class Song
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public required byte[] FileData { get; set; }

        public Guid? AlbumId { get; set; }
        public Album Album { get; set; }

        public Guid? UserId { get; set; }
        public User User { get; set; }

        public Guid? GroupId { get; set; }
        public Group Group { get; set; }
    }
}
