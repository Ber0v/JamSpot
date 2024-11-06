using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtJamWebApp.Controllers
{
    public class FeedController : Controller
    {
        private readonly JamSpotDbContext context;
        private readonly UserManager<User> _userManager;

        public FeedController(JamSpotDbContext _context, UserManager<User> userManager)
        {
            context = _context;
            _userManager = userManager;
        }

        // GET: /Feed/Index - Display all posts (MusicianSearch posts)
        [HttpGet]
        public async Task<IActionResult> All()
        {
            var model = await context.Posts
                .Include(p => p.User)
                .Select(p => new FeedViewModel()
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Image = p.User != null ? p.User.ProfilePicture : (p.Group != null ? p.Group.Logo : null),
                    Publisher = p.User != null ? p.User.UserName : (p.Group != null ? p.Group.GroupName : null),
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
            var model = new CreatePostViewModel();
            return View(model);
        }

        // POST: /Feed/CreatePost - Handle form submission for musician search posts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(CreatePostViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                var post = new Post
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedDate = model.CreatedDate,
                    User = user
                };

                context.Posts.Add(post);
                await context.SaveChangesAsync();

                return RedirectToAction("All");
            }

            return View(model);
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
