namespace JamSpotApp.Models
{
    public class AddMusicianViewModel
    {
        public Guid GroupId { get; set; }
        public required List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public string SearchQuery { get; set; }
    }
}
