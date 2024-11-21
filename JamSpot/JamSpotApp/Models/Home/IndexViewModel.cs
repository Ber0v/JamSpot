using JamSpotApp.Models.Event;
using JamSpotApp.Models.feed;

namespace JamSpotApp.Models.Home
{
    public class IndexViewModel
    {
        public List<EventViewModel> Events { get; set; }
        public List<FeedViewModel> Posts { get; set; }
    }
}
