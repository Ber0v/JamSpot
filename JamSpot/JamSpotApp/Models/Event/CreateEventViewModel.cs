using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Event
{
    public class CreateEventViewModel
    {
        public Guid Id { get; set; } // Добавено за редактиране

        [Required(ErrorMessage = "The event name is required.")]
        [StringLength(50, ErrorMessage = "The event name cannot exceed 50 characters.", MinimumLength = 1)]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "The event description is required.")]
        [StringLength(300, ErrorMessage = "The event description cannot exceed 300 characters.", MinimumLength = 1)]
        public string EventDescription { get; set; } = string.Empty;

        [Range(0, 999.00, ErrorMessage = "The price must be a positive value and cannot exceed 999.")]
        public double Price { get; set; }

        [Required(ErrorMessage = "The location is required.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "The date is required.")]
        [RegularExpression(@"^\d{2}\.\d{2}\.\d{4}$", ErrorMessage = "Date must be in the format dd.MM.yyyy.")]
        public string Date { get; set; } = DateTime.Today.ToString("dd.MM.yyyy");

        [Required(ErrorMessage = "The time is required.")]
        [RegularExpression(@"^\d{2}\:\d{2}$", ErrorMessage = "Time must be in the format HH:mm.")]
        public string Hour { get; set; } = TimeOnly.FromDateTime(DateTime.Now).ToString("HH\\:mm");
    }
}
