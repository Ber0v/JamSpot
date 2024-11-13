using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    using JamSpotApp.Models.Event;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using System.Globalization;

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
                .Include(e => e.Organizer)
                .Where(e => e.Date >= DateTime.Today)
                .OrderBy(e => e.Date)
                .Select(e => new EventViewModel()
                {
                    Id = e.Id,
                    EventName = e.EventName,
                    EventDescription = e.EventDescription,
                    Organizer = e.Organizer.UserName,
                    Location = e.Location,
                    Date = e.Date.ToString("dd-MM-yy"),
                })
                .AsNoTracking()
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            var model = new CreateEventViewModel();
            return View(model);
        }

        // POST: /Event/CreateEvent 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(CreateEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                DateTime Date;

                if (DateTime.TryParseExact(model.Date, "dd-MM-yy", CultureInfo.CurrentCulture, DateTimeStyles.None, out Date) == false)
                {
                    ModelState.AddModelError(nameof(model.Date), "Invalid date format");

                    return View(model);
                }

                var events = new Event
                {
                    EventName = model.EventName,
                    EventDescription = model.EventDescription,
                    Location = model.Location,
                    Date = Date,
                    Organizer = user
                };

                context.Events.Add(events);
                await context.SaveChangesAsync();

                return RedirectToAction("All");
            }

            return View(model);
        }

        // GET: /Event/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var model = await context.Events
                .Where(p => p.Id == id)
                .AsNoTracking()
                .Select(e => new DeleteEventViewModel()
                {
                    Id = e.Id,
                    EventName = e.EventName,
                    Organizer = e.Organizer.UserName
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // POST: /Event/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DeleteEventViewModel model)
        {
            var eventToDelete = await context.Events.FindAsync(model.Id);

            if (eventToDelete == null)
            {
                return NotFound();
            }

            context.Events.Remove(eventToDelete);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }
    }
}
