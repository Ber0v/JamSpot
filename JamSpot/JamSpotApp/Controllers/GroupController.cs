using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Controllers
{
    using JamSpotApp.Models.Group;
    using Microsoft.AspNetCore.Identity;
    public class GroupController : Controller
    {
        private readonly JamSpotDbContext context;
        private readonly UserManager<User> _userManager;

        public GroupController(JamSpotDbContext _context, UserManager<User> userManager)
        {
            context = _context;
            _userManager = userManager;
        }
        public async Task<IActionResult> All()
        {
            var model = await context.Groups
                .Include(p => p.Creator)
                .Select(p => new GroupViewModel()
                {
                    Id = p.Id,
                    GroupName = p.GroupName,
                    Logo = p.Logo != null ? p.Logo : null,
                    Genre = p.Genre,
                })
                .AsNoTracking()
                .ToListAsync();

            return View(model);
        }
    }
}
