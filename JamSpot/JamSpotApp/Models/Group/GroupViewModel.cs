using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Models.Group
{
    public class GroupViewModel
    {
        public Guid Id { get; set; }

        [Required]
        public required string GroupName { get; set; }
        public string? Logo { get; set; }
        public string? Genre { get; set; }
    }
}
