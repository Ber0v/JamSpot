namespace JamSpotApp.Models
{
    public class MusicianViewModel
    {
        public Guid Id { get; set; }
        public required string UserName { get; set; }
        public required string Instrument { get; set; }
    }

}
