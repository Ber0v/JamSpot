namespace JamSpotApp.Models.User
{
    public class UserViewModel
    {
        public Guid Id { get; set; }
        public required string ProfilePicture { get; set; }
        public required string UserName { get; set; }
        public string? UserBio { get; set; }
        public string? Instrument { get; set; }
    }
}
