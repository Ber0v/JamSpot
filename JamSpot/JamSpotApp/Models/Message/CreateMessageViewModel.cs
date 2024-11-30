using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Message
{
    public class CreateMessageViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "The title must be between 5 and 200 characters.", MinimumLength = 5)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is mandatory.")]
        [StringLength(1000, ErrorMessage = "The content must be between 5 and 1000 characters.", MinimumLength = 5)]
        public string Content { get; set; } = string.Empty;
    }
}
