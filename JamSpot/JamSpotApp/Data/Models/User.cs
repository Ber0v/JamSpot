using Microsoft.AspNetCore.Identity;

namespace JamSpotApp.Data.Models
{
    public class User : IdentityUser<Guid>
    {
        public User()
        {
            this.Id = Guid.NewGuid();
        }
    }
}
