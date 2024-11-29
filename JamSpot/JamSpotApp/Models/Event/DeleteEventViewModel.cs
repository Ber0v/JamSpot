using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Event
{
    public class DeleteEventViewModel
    {
        public Guid Id { get; set; }

        [Required]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public string Organizer { get; set; } = string.Empty;
    }
}
