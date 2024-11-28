namespace JamSpotApp.Models.Group
{
    public class GroupDetailsViewModel
    {
        public Guid Id { get; set; }
        public required string GroupName { get; set; }
        public required string Logo { get; set; }
        public required string Description { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? Genre { get; set; }
        public required string Creator { get; set; }
        public Guid CreatorId { get; set; }
        public bool IsGroupAdmin { get; set; }
        public List<GroupMemberViewModel> Members { get; set; } = new List<GroupMemberViewModel>();
        public List<UserSelectionViewModel> AvailableUsers { get; set; } = new List<UserSelectionViewModel>();
    }
}
