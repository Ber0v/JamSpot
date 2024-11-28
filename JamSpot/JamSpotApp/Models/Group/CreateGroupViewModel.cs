using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Group
{
    public class CreateGroupViewModel
    {
        public Guid Id { get; set; } // Optional или може да бъде премахнато при създаване

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string GroupName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 5)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Instagram Profile URL")]
        [Url]
        public string? InstagramUrl { get; set; }

        [Display(Name = "Facebook Profile URL")]
        [Url]
        public string? FacebookUrl { get; set; }

        public IFormFile? Logo { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string Genre { get; set; }

        public string? ExistingLogoPath { get; set; }
    }
}
