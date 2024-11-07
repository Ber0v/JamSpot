using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models
{
    public class CreatePostViewModel
    {
        public Guid Id { get; internal set; }

        [Required]
        [StringLength(50), MinLength(1)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000), MinLength(10)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
