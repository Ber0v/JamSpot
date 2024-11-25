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

        public async Task<IActionResult> All(int pageNumber = 1)
        {
            int pageSize = 4; // Брой на събитията на страница

            var user = await _userManager.GetUserAsync(User);

            var userHasGroup = await context.Groups
                .AnyAsync(g => g.CreatorId == user.Id);

            var isMemberOfGroup = await context.Groups
                .AnyAsync(g => g.Members.Any(m => m.Id == user.Id));

            // Общо брой на събитията
            var totalEvents = await context.Events.CountAsync(e => e.Date >= DateTime.Today);

            // Изчисляване на общия брой страници
            int totalPages = (int)Math.Ceiling(totalEvents / (double)pageSize);

            // Извличане на събитията за текущата страница
            var events = await context.Events
                .Include(e => e.Organizer)
                .Where(e => e.Date >= DateTime.Today)
                .OrderBy(e => e.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EventViewModel()
                {
                    Id = e.Id,
                    EventName = e.EventName,
                    EventDescription = e.EventDescription,
                    Price = e.Price,
                    Organizer = e.Organizer.UserName,
                    Location = e.Location,
                    Date = e.Date.ToString("dd.MM.yyyy"),
                    Hour = e.Hour.ToString("HH\\:mm"),
                })
                .AsNoTracking()
                .ToListAsync();

            ViewBag.UserHasGroup = userHasGroup;
            ViewBag.IsMemberOfGroup = isMemberOfGroup;

            var model = new EventListViewModel
            {
                Events = events,
                CurrentPage = pageNumber,
                TotalPages = totalPages
            };

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
                TimeOnly hour;

                if (DateTime.TryParseExact(model.Date, "dd.MM.yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out Date) == false)
                {
                    ModelState.AddModelError(nameof(model.Date), "Invalid date format");
                    return View(model);
                }

                if (TimeOnly.TryParseExact(model.Hour, "HH\\:mm", CultureInfo.CurrentCulture, DateTimeStyles.None, out hour) == false)
                {
                    ModelState.AddModelError(nameof(model.Hour), "Invalid time format");
                    return View(model);
                }

                var events = new Event
                {
                    EventName = model.EventName,
                    EventDescription = model.EventDescription,
                    Price = model.Price,
                    Location = model.Location,
                    Date = Date,
                    Hour = hour,
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
            var eventToDelete = await context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventToDelete == null)
            {
                return NotFound();
            }

            // Проверка дали текущият потребител е организатор или администратор
            var currentUser = await _userManager.GetUserAsync(User);
            if (eventToDelete.Organizer.Id != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var model = new DeleteEventViewModel
            {
                Id = eventToDelete.Id,
                EventName = eventToDelete.EventName,
                Organizer = eventToDelete.Organizer.UserName
            };

            return View(model);
        }

        // POST: /Event/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DeleteEventViewModel model)
        {
            var eventToDelete = await context.Events
                .Include(e => e.Organizer) // Включваме организатора за проверка
                .FirstOrDefaultAsync(e => e.Id == model.Id);

            if (eventToDelete == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (eventToDelete.Organizer.Id != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid(); // Забранен достъп
            }

            context.Events.Remove(eventToDelete);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }
    }
}
