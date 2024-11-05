using JamSpotApp.Data;
using JamSpotApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtJamWebApp.Controllers
{
    public class FeedController : Controller
    {
        private readonly JamSpotDbContext context;

        public FeedController(JamSpotDbContext _context)
        {
            context = _context;
        }

        // GET: /Feed/Index - Display all posts (MusicianSearch posts)
        [HttpGet]
        public async Task<IActionResult> All()
        {
            var model = await context.Posts
                .Select(p => new FeedViewModel()
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    ProfileImage = p.ProfileImage,
                    Publisher = p.User != null ? p.User.UserName : p.Group.GroupName,
                    CreatedDate = p.CreatedDate.ToString("yyyy-MM-dd"),
                })
                .AsNoTracking()
                .ToListAsync();

            return View(model);
        }

        // GET: /Feed/CreatePost - Display form for creating a musician search post
        [HttpGet]
        public IActionResult CreatePost()
        {
            return View();
        }

        // POST: /Feed/CreatePost - Handle form submission for musician search posts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            return RedirectToAction("All");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Edit()
        {
            return RedirectToAction(nameof(All));
        }
    }
}
