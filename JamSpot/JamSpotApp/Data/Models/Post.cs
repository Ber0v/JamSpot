namespace JamSpotApp.Data.Models
{
    public class Post
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Content { get; set; }
        public PostTypeEnum PostType { get; set; }
        public DateTime CreatedDate { get; set; }

        public Guid? UserId { get; set; }
        public User User { get; set; }

        public Guid? GroupId { get; set; }
        public Group Group { get; set; }
    }
    public enum PostTypeEnum
    {
        SeekingMusicians,
        UploadedSong,
        GeneralAnnouncement
    }
}
