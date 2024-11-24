using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Data
{
    public class JamSpotDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public JamSpotDbContext(DbContextOptions<JamSpotDbContext> options)
            : base(options)
        {
        }

        public new DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Album> Albums { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Post relationships
            PostMethod(builder);

            // Configure Event relationships
            builder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(g => g.Events)
                .HasForeignKey(e => e.OrganizerId);

            builder.Entity<Group>()
                .HasMany(g => g.Members)
                .WithMany(u => u.MemberOfGroups)
                .UsingEntity<Dictionary<string, object>>(
                    "UserGroup",
                    j => j
                        .HasOne<User>()
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j => j
                        .HasOne<Group>()
                        .WithMany()
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j =>
                    {
                        j.HasKey("UserId", "GroupId");
                        j.ToTable("UserGroups");
                    }
                );

            builder.Entity<Group>()
                .HasOne(g => g.Creator)
                .WithMany(u => u.CreatedGroups)
                .HasForeignKey(g => g.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);


            // Configure Song relationships
            SongMethod(builder);
        }

        private static void SongMethod(ModelBuilder builder)
        {
            builder.Entity<Song>()
                            .HasOne(s => s.User)
                            .WithMany(u => u.Songs)
                            .HasForeignKey(s => s.UserId);

            builder.Entity<Song>()
                .HasOne(s => s.Group)
                .WithMany(g => g.Songs)
                .HasForeignKey(s => s.GroupId);

            builder.Entity<Song>()
                .HasOne(s => s.Album)
                .WithMany(a => a.Songs)
                .HasForeignKey(s => s.AlbumId);
        }

        private static void PostMethod(ModelBuilder builder)
        {
            builder.Entity<Post>()
                            .HasOne(p => p.User)
                            .WithMany(u => u.Posts)
                            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Post>()
                .HasOne(p => p.Group)
                .WithMany(g => g.Posts)
                .HasForeignKey(p => p.GroupId);
        }

    }
}
