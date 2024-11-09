namespace JamSpotApp.Models.User
{
    public class EditUserViewModel
    {
        public Guid Id { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public required string UserName { get; set; }
        public string? UserBio { get; set; }
        public string? Instrument { get; set; }

        public string? ExistingPicturePath { get; set; }

    }
}
