using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersModel : PageModel
    {
        private readonly JamSpotDbContext _context;

        public AdminUsersModel(JamSpotDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<User> Users { get; set; }

        public void OnGet()
        {
            Users = _context.Users
                .Where(u => string.IsNullOrEmpty(SearchTerm) || u.UserName.Contains(SearchTerm))
                .ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                // Зареждане на потребителя със свързаните данни
                var user = await _context.Users
                    .Include(u => u.Events)
                    .Include(u => u.Posts)
                    .Include(u => u.CreatedGroups)
                        .ThenInclude(g => g.Members)
                    .Include(u => u.CreatedGroups)
                        .ThenInclude(g => g.Posts)
                    .Include(u => u.MemberOfGroups)
                        .ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound();
                }

                // 1. Изтриване на постовете на потребителя
                if (user.Posts != null && user.Posts.Any())
                {
                    _context.Posts.RemoveRange(user.Posts);
                }

                // 2. Изтриване на събитията, създадени от потребителя
                if (user.Events != null && user.Events.Any())
                {
                    _context.Events.RemoveRange(user.Events);
                }

                // 3. Изтриване на групите, създадени от потребителя
                if (user.CreatedGroups != null && user.CreatedGroups.Any())
                {
                    foreach (var group in user.CreatedGroups)
                    {
                        // Премахване на всички членове от групата
                        if (group.Members != null && group.Members.Any())
                        {
                            group.Members.Clear();
                        }

                        // Изтриване на постовете на групата, ако има такива
                        if (group.Posts != null && group.Posts.Any())
                        {
                            _context.Posts.RemoveRange(group.Posts);
                        }

                        _context.Groups.Remove(group);
                    }
                }

                // 4. Премахване на потребителя от всички групи, в които е член
                if (user.MemberOfGroups != null && user.MemberOfGroups.Any())
                {
                    foreach (var group in user.MemberOfGroups)
                    {
                        group.Members.Remove(user);
                    }
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToPage();
            }
        }
    }
}
