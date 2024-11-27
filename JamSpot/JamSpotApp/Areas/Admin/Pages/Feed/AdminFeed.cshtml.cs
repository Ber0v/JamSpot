using JamSpotApp.Data;
using JamSpotApp.Models.feed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JamSpotApp.Areas.Admin.Pages.Feed
{
    using Microsoft.EntityFrameworkCore;

    [Authorize(Roles = "Admin")]
    public class AdminFeedModel : PageModel
    {
        private readonly JamSpotDbContext _context;

        public AdminFeedModel(JamSpotDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<FeedViewModel> Posts { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .AsQueryable();

            // Ако SearchTerm не е празен, добавяме филтър
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                string trimmedSearchTerm = SearchTerm.Trim();

                query = query.Where(p =>
                    (p.User != null && p.User.UserName.Contains(trimmedSearchTerm)) ||
                    (p.Group != null && p.Group.GroupName.Contains(trimmedSearchTerm))
                );
            }

            Posts = await query
                .OrderByDescending(e => e.CreatedDate)
                .Select(e => new FeedViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Content = e.Content,
                    Instrument = e.User != null ? e.User.Instrument : null,
                    Image = e.User != null ? e.User.ProfilePicture : (e.Group != null ? e.Group.Logo : null),
                    Publisher = e.User != null ? e.User.UserName : (e.Group != null ? e.Group.GroupName : "Unknown"),
                    PublisherId = e.User != null ? e.User.Id : e.GroupId ?? Guid.Empty,
                    CreatedDate = e.CreatedDate.ToString("yyyy-MM-dd"),
                    IsGroupPost = e.Group != null,
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var postsToDelete = await _context.Posts.FindAsync(id);

            if (postsToDelete == null)
            {
                return NotFound();
            }

            _context.Posts.Remove(postsToDelete);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
