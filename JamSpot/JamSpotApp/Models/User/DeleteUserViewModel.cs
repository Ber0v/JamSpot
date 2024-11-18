namespace JamSpotApp.Models.User
{
    public class DeleteUserViewModel
    {
        public Guid Id { get; set; }

        public required string UserName { get; set; }
    }
}
