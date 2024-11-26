using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public IActionResult OnPostDelete(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return RedirectToPage("./Index");
        }
    }
}
