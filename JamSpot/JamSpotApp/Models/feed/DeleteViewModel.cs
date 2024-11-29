using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.feed
{
    public class DeleteViewModel
    {
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Publisher { get; set; } = string.Empty;
    }
}
