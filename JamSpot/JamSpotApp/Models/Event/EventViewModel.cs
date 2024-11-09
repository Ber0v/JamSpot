namespace JamSpotApp.Models.Event
{
    public class EventViewModel
    {
        public required string EventName { get; set; }
        public required string EventDescription { get; set; }
        public required string Location { get; set; }
        public required string Date { get; set; } = DateTime.Today.ToString("dd-MM-yy");
        public required string Organizer { get; set; }

    }
}
