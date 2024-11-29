using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Event
{
    public class EventViewModel
    {
        public Guid Id { get; set; }

        [Required]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public string EventDescription { get; set; } = string.Empty;

        [Required]
        public double Price { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Date { get; set; } = string.Empty;

        [Required]
        public string Hour { get; set; } = string.Empty;

        [Required]
        public string Organizer { get; set; } = string.Empty;

        public Guid OrganizerId { get; set; } // Добавено за проверка в view

    }
}
