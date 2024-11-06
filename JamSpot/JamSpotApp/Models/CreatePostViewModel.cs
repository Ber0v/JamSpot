using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models
{
    public class CreatePostViewModel
    {
        [Required]
        [StringLength(10), MinLength(2)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000), MinLength(10)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
