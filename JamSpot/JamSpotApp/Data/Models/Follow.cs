namespace JamSpotApp.Data.Models
{
    public class Follow
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FollowerId { get; set; }
        public Guid FollowedId { get; set; }
    }
}
