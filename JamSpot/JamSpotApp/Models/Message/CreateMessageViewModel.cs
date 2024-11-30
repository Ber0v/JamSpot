using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Message
{
    public class CreateMessageViewModel
    {
        public string? Title { get; set; }

        [Required(ErrorMessage = "Content is mandatory.")]
        [StringLength(1000, ErrorMessage = "The content must be between 5 and 1000 characters.", MinimumLength = 5)]
        public string Content { get; set; } = string.Empty;
    }
}
