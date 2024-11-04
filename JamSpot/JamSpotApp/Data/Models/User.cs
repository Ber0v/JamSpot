using Microsoft.AspNetCore.Identity;

namespace JamSpotApp.Data.Models
{
    public class User : IdentityUser<Guid>
    {
        public User()
        {
            this.Id = Guid.NewGuid();
        }

        public required string Username { get; set; }

        public required byte[] ProfilePicture { get; set; }
        public string? UserBio { get; set; }
        public string? Instrument { get; set; }
        public UserType UserType { get; set; }

        public ICollection<User> FollowingUsers { get; set; } = new HashSet<User>();
        public ICollection<Group> FollowingGroups { get; set; } = new HashSet<Group>();
        public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
        public ICollection<Song> Songs { get; set; } = new HashSet<Song>();
    }

    public enum UserType
    {
        Musician,
        GeneralUser
    }
}