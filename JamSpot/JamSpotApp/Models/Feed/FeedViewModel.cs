using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Feed
{
    public class FeedViewModel
    {
        public Guid Id { get; set; }

        public string? Image { get; set; }

        public required string Title { get; set; }

        [Required]
        public required string Content { get; set; }

        [Required]
        public required string Publisher { get; set; }

        [Required]
        public string CreatedDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    }
}
