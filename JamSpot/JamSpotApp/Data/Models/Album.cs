namespace JamSpotApp.Data.Models
{
    public class Album
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public required byte[] CoverImage { get; set; }

        public ICollection<Song> Songs { get; set; } = new HashSet<Song>();
        public Guid? UserId { get; set; }
        public User User { get; set; }

        public Guid? GroupId { get; set; }
        public Group Group { get; set; }
    }
}
