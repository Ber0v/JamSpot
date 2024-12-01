using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace JamSpotApp.Controllers
{
    [Authorize]
    public class EventController : Controller
    {
        private readonly JamSpotDbContext context;
        private readonly UserManager<User> _userManager;

        public EventController(JamSpotDbContext _context, UserManager<User> userManager)
        {
            context = _context;
            _userManager = userManager;
        }

        // GET: /Event/All - Display all upcoming events with pagination
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> All(string searchTerm, string filter, int pageNumber = 1)
        {
            ViewData["SearchTerm"] = searchTerm;
            ViewData["CurrentFilter"] = filter;

            int pageSize = 4; // Брой събития на страница

            var user = await _userManager.GetUserAsync(User);

            bool userHasGroup = false;
            bool isMemberOfGroup = false;

            if (user != null)
            {
                userHasGroup = await context.Groups
                    .AnyAsync(g => g.CreatorId == user.Id);

                isMemberOfGroup = await context.Groups
                    .AnyAsync(g => g.Members.Any(m => m.Id == user.Id));
            }

            // Започваме със заявка за всички събития
            IQueryable<Event> query = context.Events
                .Include(e => e.Organizer);

            if (string.IsNullOrEmpty(filter) || filter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Date >= DateTime.Today);
            }
            else if (filter.Equals("old", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Date < DateTime.Today);
            }
            else
            {
                if (filter.Equals("free", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(e => e.Price == 0 && e.Date >= DateTime.Today);
                }
                else if (filter.Equals("paid", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(e => e.Price > 0 && e.Date >= DateTime.Today);
                }
            }

            // Ако има въведен searchTerm, филтрираме събитията по него
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(e => e.EventName.ToLower().Contains(lowerSearchTerm)
                                       || e.Organizer.UserName.ToLower().Contains(lowerSearchTerm));
            }

            var totalEvents = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(totalEvents / (double)pageSize);

            var events = await query
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
                    OrganizerId = e.Organizer.Id,
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

        // GET: /Event/CreateEvent - Display form for creating a new event
        [HttpGet]
        public IActionResult CreateEvent()
        {
            var model = new CreateEventViewModel();
            return View(model);
        }

        // POST: /Event/CreateEvent - Handle form submission for creating a new event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(CreateEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                DateTime date;
                TimeOnly hour;

                if (!DateTime.TryParseExact(model.Date, "dd.MM.yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                {
                    ModelState.AddModelError(nameof(model.Date), "Invalid date format.");
                    return View(model);
                }

                if (!TimeOnly.TryParseExact(model.Hour, "HH\\:mm", CultureInfo.CurrentCulture, DateTimeStyles.None, out hour))
                {
                    ModelState.AddModelError(nameof(model.Hour), "Invalid time format.");
                    return View(model);
                }

                var newEvent = new Event
                {
                    EventName = model.EventName,
                    EventDescription = model.EventDescription,
                    Price = model.Price,
                    Location = model.Location,
                    Date = date,
                    Hour = hour,
                    Organizer = user
                };

                context.Events.Add(newEvent);
                try
                {
                    await context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "The event was successfully created.";
                }
                catch (Exception ex)
                {
                    // Log the error (implement logging as needed)
                    ModelState.AddModelError("", "An error occurred while creating the event. Please try again.");
                    return View(model);
                }

                return RedirectToAction("All");
            }

            // If model state is invalid, return the view with validation messages
            return View(model);
        }

        // GET: /Event/Edit/{id} - Display form to edit an event
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var ev = await context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (ev.Organizer.Id != currentUser.Id)
            {
                return Forbid();
            }

            var model = new CreateEventViewModel
            {
                Id = ev.Id,
                EventName = ev.EventName,
                EventDescription = ev.EventDescription,
                Price = ev.Price,
                Location = ev.Location,
                Date = ev.Date.ToString("dd.MM.yyyy"),
                Hour = ev.Hour.ToString("HH\\:mm")
            };

            return View(model);
        }

        // POST: /Event/Edit/{id} - Handle form submission for editing an event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CreateEventViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var ev = await context.Events
                    .Include(e => e.Organizer)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (ev == null)
                {
                    return NotFound();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (ev.Organizer.Id != currentUser.Id)
                {
                    return Forbid();
                }

                DateTime date;
                TimeOnly hour;

                if (!DateTime.TryParseExact(model.Date, "dd.MM.yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                {
                    ModelState.AddModelError(nameof(model.Date), "Invalid date format.");
                    return View(model);
                }

                if (!TimeOnly.TryParseExact(model.Hour, "HH\\:mm", CultureInfo.CurrentCulture, DateTimeStyles.None, out hour))
                {
                    ModelState.AddModelError(nameof(model.Hour), "Invalid time format.");
                    return View(model);
                }

                ev.EventName = model.EventName;
                ev.EventDescription = model.EventDescription;
                ev.Price = model.Price;
                ev.Location = model.Location;
                ev.Date = date;
                ev.Hour = hour;

                try
                {
                    await context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "The event was successfully edited.";
                }
                catch (Exception ex)
                {
                    // Log the error (implement logging as needed)
                    ModelState.AddModelError("", "An error occurred while editing the event. Please try again.");
                    return View(model);
                }

                return RedirectToAction("All");
            }

            // If model state is invalid, return the view with validation messages
            return View(model);
        }

        // GET: /Event/Delete/{id} - Display confirmation form to delete an event
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ev = await context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (ev.Organizer.Id != currentUser.Id)
            {
                return Forbid();
            }

            var model = new DeleteEventViewModel
            {
                Id = ev.Id,
                EventName = ev.EventName,
                Organizer = ev.Organizer.UserName
            };

            return View(model);
        }

        // POST: /Event/DeleteConfirmed - Handle event deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DeleteEventViewModel model)
        {
            var ev = await context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == model.Id);

            if (ev == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (ev.Organizer.Id != currentUser.Id)
            {
                return Forbid();
            }

            context.Events.Remove(ev);
            try
            {
                await context.SaveChangesAsync();
                TempData["SuccessMessage"] = "The event was successfully deleted.";
            }
            catch (Exception ex)
            {
                // Log the error (implement logging as needed)
                TempData["ErrorMessage"] = "An error occurred while deleting the event. Please try again.";
                return RedirectToAction("All");
            }

            return RedirectToAction("All");
        }

        // GET: /Event/Details/{id} - Display details of an event
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var ev = await context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
            {
                return NotFound();
            }

            var model = new EventViewModel
            {
                Id = ev.Id,
                EventName = ev.EventName,
                EventDescription = ev.EventDescription,
                Price = ev.Price,
                Location = ev.Location,
                Date = ev.Date.ToString("dd.MM.yyyy"),
                Hour = ev.Hour.ToString("HH\\:mm"),
                Organizer = ev.Organizer.UserName,
                OrganizerId = ev.Organizer.Id
            };

            return View(model);
        }
    }

    // Допълнителен помощен клас за извличане на текущия потребител ID
    public static class ContextExtensions
    {
        public static async Task<Guid> GetUserIdAsync(this JamSpotDbContext context, ClaimsPrincipal user, UserManager<User> userManager)
        {
            var currentUser = await userManager.GetUserAsync(user);
            return currentUser != null ? currentUser.Id : Guid.Empty;
        }
    }
}
