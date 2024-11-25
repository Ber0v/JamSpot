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
            var currentUser = await _userManager.GetUserAsync(User);

            var model = await context.Posts
                .Include(p => p.User)
                .Select(p => new FeedViewModel()
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Instrument = p.User.Instrument,
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
            var post = await context.Posts
        .Include(p => p.User)
        .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User.Id != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }

            var model = new CreatePostViewModel
            {
                Title = post.Title,
                Content = post.Content
            };

            return View(model);
        }

        // POST: Feed/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(CreatePostViewModel model, Guid id)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var post = await context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User.Id != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }

            post.Title = model.Title;
            post.Content = model.Content;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var post = await context.Posts
        .Include(p => p.User)
        .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User.Id != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }

            var model = new DelateViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Publisher = post.User?.UserName
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(DelateViewModel model)
        {
            var post = await context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == model.Id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User.Id != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }

            context.Posts.Remove(post);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }
    }
}
