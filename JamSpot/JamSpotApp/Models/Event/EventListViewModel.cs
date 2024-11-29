namespace JamSpotApp.Models.Event
{
    public class EventListViewModel
    {
        public IEnumerable<EventViewModel> Events { get; set; } = new List<EventViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
