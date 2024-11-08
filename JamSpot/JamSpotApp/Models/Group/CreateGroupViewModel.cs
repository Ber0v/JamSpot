using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Group
{
    public class CreateGroupViewModel
    {
        public Guid Id { get; internal set; }

        [Required]
        [StringLength(50), MinLength(2)]
        public string GroupName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000), MinLength(5)]
        public string Description { get; set; } = string.Empty;

        public IFormFile? Logo { get; set; }

        [Required]
        [StringLength(20), MinLength(2)]
        public string? Genre { get; set; }

        public string? ExistingLogoPath { get; set; }
    }
}
