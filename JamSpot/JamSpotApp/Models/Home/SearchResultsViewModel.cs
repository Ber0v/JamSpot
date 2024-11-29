namespace JamSpotApp.Models.Home
{
    public class SearchResultsViewModel
    {
        public List<UserResultViewModel> Users { get; set; } = new List<UserResultViewModel>();
        public List<GroupResultViewModel> Groups { get; set; } = new List<GroupResultViewModel>();
    }
}
