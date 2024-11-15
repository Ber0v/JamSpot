namespace JamSpotApp.Models.Event
{
    public class EventViewModel
    {
        public Guid Id { get; set; }
        public required string EventName { get; set; }
        public required string EventDescription { get; set; }
        public required double Price { get; set; }
        public required string Location { get; set; }
        public required string Date { get; set; }
        public required string Hour { get; set; }
        public required string Organizer { get; set; }

    }
}
