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
        private readonly ILogger<FeedController> _logger;

        public FeedController(JamSpotDbContext _context, UserManager<User> userManager, ILogger<FeedController> logger)
        {
            context = _context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Feed/All - Display all posts
        [HttpGet]
        public async Task<IActionResult> All(int pageNumber = 1)
        {
            int PageSize = 6;

            var currentUser = await _userManager.GetUserAsync(User);

            // Намиране на групата, към която потребителят принадлежи или която е създател
            var userGroup = await context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == currentUser.Id) || g.CreatorId == currentUser.Id);

            // Общо броя на постовете
            int totalPosts = await context.Posts.CountAsync();

            // Изчисляване на общия брой страници
            int totalPages = (int)Math.Ceiling(totalPosts / (double)PageSize);

            // Вземане на постовете за текущата страница, сортирани по дата (най-новите първи)
            var posts = await context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedDate)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new FeedViewModel()
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Instrument = p.User != null ? p.User.Instrument : null,
                    Image = p.User != null ? p.User.ProfilePicture : (p.Group != null ? p.Group.Logo : null),
                    Publisher = p.User != null ? p.User.UserName : (p.Group != null ? p.Group.GroupName : "Unknown"),
                    PublisherId = p.User != null ? p.User.Id : p.GroupId ?? Guid.Empty,
                    CreatedDate = p.CreatedDate.ToString("yyyy-MM-dd"),
                    IsGroupPost = p.Group != null,
                    // Задаване на CanEdit
                    CanEdit = (p.User != null && p.User.Id == currentUser.Id) ||
                              (p.Group != null && userGroup != null && (userGroup.CreatorId == currentUser.Id ||
                              userGroup.Members.Any(m => m.Id == currentUser.Id)))
                })
                .ToListAsync();

            // Създаване на FeedPageViewModel
            var viewModel = new FeedPageViewModel
            {
                Posts = posts,
                CurrentPage = pageNumber,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult UserDetails(Guid id)
        {
            // Пренасочва към UserController -> All
            return RedirectToAction("All", "User", new { id });
        }

        // GET: /Feed/CreatePost - Display form for creating a post
        [HttpGet]
        public async Task<IActionResult> CreatePost()
        {
            var user = await _userManager.GetUserAsync(User);

            // Намиране на групата, към която потребителят принадлежи или която е създател
            var userGroup = await context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == user.Id) || g.CreatorId == user.Id);

            var model = new CreatePostViewModel();

            if (userGroup != null)
            {
                // Потребителят е член или създател на група, така че можем да позволим публикуване от името на групата
                ViewBag.UserGroup = userGroup.GroupName;
                ViewBag.IsGroupCreator = userGroup.CreatorId == user.Id;
            }
            else
            {
                // Потребителят не принадлежи на никаква група, така че не може да публикува като група
                model.IsGroupPost = false;
            }

            return View(model);
        }

        // POST: /Feed/CreatePost - Handle form submission for posts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(CreatePostViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                // Намиране на групата, към която потребителят принадлежи или която е създател
                var userGroup = await context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == user.Id) || g.CreatorId == user.Id);

                var post = new Post
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedDate = model.CreatedDate
                };

                if (model.IsGroupPost)
                {
                    if (userGroup == null)
                    {
                        ModelState.AddModelError("", "Не сте член или създател на никаква група.");
                        return View(model);
                    }

                    // Проверка дали потребителят е създател или член на групата
                    bool isGroupCreator = userGroup.CreatorId == user.Id;
                    bool isGroupMember = userGroup.Members.Any(m => m.Id == user.Id);

                    if (!isGroupCreator && !isGroupMember)
                    {
                        ModelState.AddModelError("", "Нямате права да публикувате от името на групата.");
                        return View(model);
                    }

                    post.GroupId = userGroup.Id;
                }
                else
                {
                    post.UserId = user.Id;
                }

                context.Posts.Add(post);
                await context.SaveChangesAsync();

                return RedirectToAction("All");
            }

            // Ако моделът не е валиден, повторно зареждане на информация за групата
            var currentUser = await _userManager.GetUserAsync(User);
            var currentGroup = await context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == currentUser.Id) || g.CreatorId == currentUser.Id);

            if (currentGroup != null)
            {
                ViewBag.UserGroup = currentGroup.GroupName;
                ViewBag.IsGroupCreator = currentGroup.CreatorId == currentUser.Id;
            }

            return View(model);
        }

        // Get: Feed/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var post = await context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User != null && post.User.Id != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }
            else if (post.Group != null && post.Group.CreatorId != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }

            var model = new CreatePostViewModel
            {
                Title = post.Title,
                Content = post.Content,
                IsGroupPost = post.Group != null
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

            var post = await context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (post.User != null)
            {
                // Ако е публикация от потребител, проверка дали текущият потребител е създателят
                if (post.User.Id != currentUser.Id)
                {
                    return Forbid();
                }
            }
            else if (post.Group != null)
            {
                // Ако е публикация от група, проверка дали текущият потребител е администратор на групата
                if (post.Group.CreatorId != currentUser.Id)
                {
                    return Forbid();
                }
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
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User != null && post.User.Id != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }
            else if (post.Group != null && post.Group.CreatorId != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }

            var model = new DelateViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Publisher = post.User != null ? post.User.UserName : post.Group.GroupName
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(DelateViewModel model)
        {
            var post = await context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User != null && post.User.Id != currentUser.Id)
            {
                return Forbid(); // или return Unauthorized();
            }
            else if (post.Group != null && post.Group.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            context.Posts.Remove(post);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }
    }
}
