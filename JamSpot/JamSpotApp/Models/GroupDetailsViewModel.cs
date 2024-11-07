namespace JamSpotApp.Models
{
    public class GroupDetailsViewModel
    {
        public Guid Id { get; set; }
        public required string GroupName { get; set; }
        public required string Logo { get; set; }
        public required string Description { get; set; }
        public string? Genre { get; set; }
        public required string Creator { get; set; }
        public bool IsOwner { get; set; }

        public List<MusicianViewModel> Members { get; set; } = new List<MusicianViewModel>();
    }
}
