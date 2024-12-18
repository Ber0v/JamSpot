﻿namespace JamSpotApp.Data.Models
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string EventName { get; set; }
        public required string EventDescription { get; set; }
        public double Price { get; set; }
        public required string Location { get; set; }
        public DateTime Date { get; set; }
        public TimeOnly Hour { get; set; }
        public Guid OrganizerId { get; set; }
        public required User Organizer { get; set; }

        public ICollection<User> Participants { get; set; } = new HashSet<User>();
    }
}
