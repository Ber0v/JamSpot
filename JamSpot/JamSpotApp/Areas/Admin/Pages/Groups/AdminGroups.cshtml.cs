using JamSpotApp.Data;
using JamSpotApp.Models.Group;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Areas.Admin.Pages.Groups
{
    [Authorize(Roles = "Admin")]
    public class AdminGroupsModel : PageModel
    {
        private readonly JamSpotDbContext _context;

        public AdminGroupsModel(JamSpotDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<GroupViewModel> Groups { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var query = _context.Groups
                .Include(p => p.Creator)
                .AsQueryable();

            // Ако SearchTerm не е празен, добавяме филтър
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                string trimmedSearchTerm = SearchTerm.Trim();

                query = query.Where(p =>
                    (p.GroupName != null && p.GroupName.Contains(trimmedSearchTerm)));
            }

            Groups = await query
                .OrderBy(p => p.GroupName)
                .Select(p => new GroupViewModel
                {
                    Id = p.Id,
                    GroupName = p.GroupName,
                    Logo = p.Logo != null ? p.Logo : null,
                    Genre = p.Genre,
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var groupsToDelete = await _context.Groups
                .Include(g => g.Creator)
                .Include(g => g.Members)
                .Include(g => g.Posts)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (groupsToDelete == null)
            {
                return NotFound();
            }

            if (groupsToDelete.Posts != null && groupsToDelete.Posts.Any())
            {
                _context.Posts.RemoveRange(groupsToDelete.Posts); // Assumes Posts is a DbSet in your DbContext
            }

            groupsToDelete.Members.Clear();

            _context.Groups.Remove(groupsToDelete);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
