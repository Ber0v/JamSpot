using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Event
{
    public class CreateEventViewModel
    {
        [StringLength(50, ErrorMessage = "The event name cannot exceed 50 characters."),
         MinLength(1, ErrorMessage = "The event name must be at least 1 character.")]
        public string EventName { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "The event description cannot exceed 300 characters."),
         MinLength(1, ErrorMessage = "The event description must be at least 1 character.")]
        public string EventDescription { get; set; } = string.Empty;

        [Range(0, 999.00, ErrorMessage = "The price must be a positive value and cannot exceed 999.")]
        public double Price { get; set; }

        [Required(ErrorMessage = "The location is required.")]
        public string Location { get; set; }
        public string Date { get; set; } = DateTime.Today.ToString("dd.MM.yyyy");
        public string Hour { get; set; } = TimeOnly.FromDateTime(DateTime.Now).ToString("HH\\:mm");
    }
}
