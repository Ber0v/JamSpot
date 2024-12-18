﻿using System.ComponentModel.DataAnnotations;

namespace JamSpotApp.Data.Models
{
    public class Group
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string GroupName { get; set; }
        public required string Logo { get; set; }
        public string? Description { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }

        public ICollection<User> Members { get; set; } = new HashSet<User>();
        public string? Genre { get; set; }

        public Guid CreatorId { get; set; }
        public required User Creator { get; set; }

        public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
        public ICollection<Song> Songs { get; set; } = new HashSet<Song>();
    }
}