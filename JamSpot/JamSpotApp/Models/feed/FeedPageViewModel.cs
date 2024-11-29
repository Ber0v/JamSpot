namespace JamSpotApp.Models.feed
{
    public class FeedPageViewModel
    {
        public IEnumerable<FeedViewModel> Posts { get; set; } = new List<FeedViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        // Удобни свойства за изгледа
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
