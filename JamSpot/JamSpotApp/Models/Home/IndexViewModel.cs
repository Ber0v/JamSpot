using JamSpotApp.Models.Event;
using JamSpotApp.Models.feed;

namespace JamSpotApp.Models.Home
{
    public class IndexViewModel
    {
        public List<EventViewModel> Events { get; set; } = new List<EventViewModel>();
        public List<FeedViewModel> Posts { get; set; } = new List<FeedViewModel>();
    }
}
