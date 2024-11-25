using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.feed
{
    public class CreatePostViewModel
    {
        public Guid Id { get; internal set; }

        [Required(ErrorMessage = "Заглавието е задължително.")]
        [StringLength(50, ErrorMessage = "Заглавието трябва да е между 1 и 50 символа.", MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Съдържанието е задължително.")]
        [StringLength(1000, ErrorMessage = "Съдържанието трябва да е между 5 и 1000 символа.", MinimumLength = 5)]
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsGroupPost { get; set; }
    }
}
