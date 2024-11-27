using Microsoft.AspNetCore.Identity;

namespace JamSpotApp.Data.Models
{
    public class User : IdentityUser<Guid>
    {
        public User()
        {
            this.Id = Guid.NewGuid();
        }

        public string? ProfilePicture { get; set; }
        public string? UserBio { get; set; }
        public string? Instrument { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public bool IsMusician { get; set; } = false;

        public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
        public ICollection<Group> MemberOfGroups { get; set; } = new HashSet<Group>();
        public ICollection<Group> CreatedGroups { get; set; } = new HashSet<Group>();
        public ICollection<Song> Songs { get; set; } = new HashSet<Song>();
        public ICollection<Event> Events { get; set; } = new HashSet<Event>();
    }
}