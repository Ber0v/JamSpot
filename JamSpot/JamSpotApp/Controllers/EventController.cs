using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    using JamSpotApp.Models.Event;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public class EventController : Controller
    {
        private readonly JamSpotDbContext context;
        private readonly UserManager<User> _userManager;

        public EventController(JamSpotDbContext _context, UserManager<User> userManager)
        {
            context = _context;
            _userManager = userManager;
        }
        public async Task<IActionResult> All()
        {
            var model = await context.Events
                .Include(p => p.Organizer)
                .Select(p => new EventViewModel()
                {
                    EventName = p.EventName,
                    EventDescription = p.EventDescription,
                    Organizer = p.Organizer.GroupName,
                    Location = p.Location,
                    Date = p.Date.ToString("dd-MM-yy"),
                })
                .AsNoTracking()
                .ToListAsync();

            return View(model);
        }
    }
}
