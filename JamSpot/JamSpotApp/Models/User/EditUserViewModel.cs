using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.User
{
    public class EditUserViewModel
    {
        public Guid Id { get; set; }
        public string ExistingPicturePath { get; set; } = string.Empty;

        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Bio")]
        public string? UserBio { get; set; }

        [Display(Name = "Instrument")]
        public string? Instrument { get; set; }

        [Display(Name = "Instagram Profile URL")]
        [Url]
        public string? InstagramUrl { get; set; }

        [Display(Name = "Facebook Profile URL")]
        [Url]
        public string? FacebookUrl { get; set; }

        public IFormFile? ProfilePicture { get; set; }

    }
}
