using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.feed
{
    public class CreatePostViewModel
    {
        [Required(ErrorMessage = "The title is required.")]
        [StringLength(50, ErrorMessage = "The title must be between 1 and 50 characters.", MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is mandatory.")]
        [StringLength(1000, ErrorMessage = "The content must be between 5 and 1000 characters.", MinimumLength = 5)]
        public string Content { get; set; } = string.Empty;
        public bool IsGroupPost { get; set; }
        public string? UserGroupName { get; set; }
        public bool IsGroupCreator { get; set; }
    }
}
