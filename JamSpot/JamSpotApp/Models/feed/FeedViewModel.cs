using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.feed
{
    public class FeedViewModel
    {
        public Guid Id { get; set; }

        public string? Image { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? Instrument { get; set; }

        [Required]
        public string Publisher { get; set; } = string.Empty;

        public Guid? PublisherId { get; set; }

        [Required]
        public string CreatedDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

        public bool IsGroupPost { get; set; }

        public bool CanEdit { get; set; }
    }
}
