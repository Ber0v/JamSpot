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
    }
}
