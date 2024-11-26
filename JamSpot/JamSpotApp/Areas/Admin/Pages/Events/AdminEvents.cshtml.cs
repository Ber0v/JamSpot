using JamSpotApp.Data;
using JamSpotApp.Models.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JamSpotApp.Areas.Admin.Pages
{
    using Microsoft.EntityFrameworkCore;

    [Authorize(Roles = "Admin")]
    public class AdminEventsModel : PageModel
    {
        private readonly JamSpotDbContext _context;

        public AdminEventsModel(JamSpotDbContext context)
        {
            _context = context;
        }

        public List<EventViewModel> Events { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Events = await _context.Events
                .Include(e => e.Organizer)
                .OrderBy(e => e.Date)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    EventName = e.EventName,
                    EventDescription = e.EventDescription,
                    Price = e.Price,
                    Organizer = e.Organizer.UserName,
                    Location = e.Location,
                    Date = e.Date.ToString("dd.MM.yyyy"),
                    Hour = e.Hour.ToString("HH\\:mm")
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var eventToDelete = await _context.Events.FindAsync(id);

            if (eventToDelete == null)
            {
                return NotFound();
            }

            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
