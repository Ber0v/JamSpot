namespace JamSpotApp.Models.Group
{
    public class DeleteGroupViewModel
    {
        public Guid Id { get; set; }

        public required string GroupName { get; set; }

        public required string Creator { get; set; }
    }
}
