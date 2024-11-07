namespace JamSpotApp.Models
{
    public class UserViewModel
    {
        public Guid Id { get; set; }
        public required string UserName { get; set; }
        public string? Instrument { get; set; }
    }
}
