using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtJamWebApp.Controllers
{
    using JamSpotApp.Models.feed;
    using Microsoft.AspNetCore.Identity;
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
                    PublisherId = p.User.Id,
                    CreatedDate = p.CreatedDate.ToString("yyyy-MM-dd"),
                })
                .AsNoTracking()
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public IActionResult UserDetails(Guid id)
        {
            // Пренасочва към UserController -> All
            return RedirectToAction("All", "User", new { id });
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

        // Get: Feed/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var model = await context.Posts
                .Where(p => p.Id == id)
                .Include(p => p.User)
                .AsNoTracking()
                .Select(p => new CreatePostViewModel()
                {
                    Title = p.Title,
                    Content = p.Content,
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // POST: Feed/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(CreatePostViewModel model, Guid Id)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Post? entity = await context.Posts.FindAsync(Id);

            if (entity == null)
            {
                return NotFound();
            }

            entity.Title = model.Title;
            entity.Content = model.Content;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var model = await context.Posts
                .Where(p => p.Id == id)
                .AsNoTracking()
                .Select(p => new DelateViewModel()
                {
                    Id = p.Id,
                    Title = p.Title,
                    Publisher = p.User != null ? p.User.UserName : (p.Group != null ? p.Group.GroupName : null)
                })
                .FirstOrDefaultAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(DelateViewModel model)
        {
            var post = await context.Posts.FindAsync(model.Id);

            if (post == null)
            {
                return NotFound();
            }

            context.Posts.Remove(post);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }
    }
}
