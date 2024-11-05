using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Data.Models
{
    public class Group
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string GroupName { get; set; }
        public string? Logo { get; set; }
        public ICollection<User> Members { get; set; } = new HashSet<User>();
        public Guid CreatorId { get; set; }
        public string? Genre { get; set; }

        public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
        public ICollection<Event> Events { get; set; } = new HashSet<Event>();
        public ICollection<Song> Songs { get; set; } = new HashSet<Song>();
    }
}