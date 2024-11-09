namespace JamSpotApp.Models.Event
{
    public class CreateEventViewModel
    {
        public string EventName { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public string Location { get; set; }
        public string Date { get; set; } = DateTime.Today.ToString("dd-MM-yy");
    }
}
