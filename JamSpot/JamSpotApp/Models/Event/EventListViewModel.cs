namespace JamSpotApp.Models.Event
{
    public class EventListViewModel
    {
        public IEnumerable<EventViewModel> Events { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
