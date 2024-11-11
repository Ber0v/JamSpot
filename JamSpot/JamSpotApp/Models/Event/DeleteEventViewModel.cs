namespace JamSpotApp.Models.Event
{
    public class DeleteEventViewModel
    {
        public Guid Id { get; set; }

        public required string EventName { get; set; }

        public required string Organizer { get; set; }
    }
}
