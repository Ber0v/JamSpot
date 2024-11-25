using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.feed
{
    public class FeedViewModel
    {
        public Guid Id { get; set; }

        public string? Image { get; set; }

        public required string Title { get; set; }

        [Required]
        public required string Content { get; set; }

        public string? Instrument { get; set; }

        [Required]
        public required string Publisher { get; set; }

        public Guid? PublisherId { get; set; }

        [Required]
        public string CreatedDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
        public bool IsGroupPost { get; set; }
        public bool CanEdit { get; set; }
    }
}
