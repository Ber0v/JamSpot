using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.feed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Controllers
{
    [Authorize]
    public class FeedController : Controller
    {
        private readonly JamSpotDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<FeedController> _logger;

        public FeedController(JamSpotDbContext context, UserManager<User> userManager, ILogger<FeedController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Feed/All - Display all posts
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> All(string searchTerm, int pageNumber = 1)
        {
            ViewData["SearchTerm"] = searchTerm;

            const int PageSize = 6;

            var currentUser = await _userManager.GetUserAsync(User);
            Group? userGroup = null;

            if (currentUser != null)
            {
                // Намиране на групата, към която потребителят принадлежи или която е създател
                userGroup = await _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == currentUser.Id) || g.CreatorId == currentUser.Id);
            }

            // Започваме със заявка за всички постове
            IQueryable<Post> query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Title.Contains(searchTerm)
                          || p.User.UserName.Contains(searchTerm)
                          || p.Group.GroupName.Contains(searchTerm));
            }

            int totalPosts = await query.CountAsync();

            // Изчисляване на общия брой страници
            int totalPages = (int)Math.Ceiling(totalPosts / (double)PageSize);

            // Вземане на постовете за текущата страница, сортирани по дата (най-новите първи)
            var posts = await query
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
                    PublisherId = p.User != null ? p.User.Id : p.GroupId,
                    CreatedDate = p.CreatedDate.ToString("yyyy-MM-dd"),
                    IsGroupPost = p.Group != null,
                    CanEdit = currentUser != null && (
                                (p.User != null && p.User.Id == currentUser.Id) ||
                                (p.Group != null && userGroup != null && userGroup.CreatorId == currentUser.Id)
                              )
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

        // GET: /Feed/CreatePost - Display form for creating a post
        [HttpGet]
        public async Task<IActionResult> CreatePost()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Намиране на групата, към която потребителят принадлежи или която е създател
            var userGroup = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == user.Id) || g.CreatorId == user.Id);

            var model = new CreatePostViewModel();

            if (userGroup != null)
            {
                model.IsGroupPost = true;
                model.UserGroupName = userGroup.GroupName;
                model.IsGroupCreator = userGroup.CreatorId == user.Id;
            }
            else
            {
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
                if (user == null)
                {
                    return Challenge();
                }

                // Намиране на групата, към която потребителят принадлежи или която е създател
                var userGroup = await _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == user.Id) || g.CreatorId == user.Id);

                var post = new Post
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedDate = DateTime.Now
                };

                if (model.IsGroupPost)
                {
                    if (userGroup == null)
                    {
                        ModelState.AddModelError("", "You are not a member or creator of any group.");
                        return View(model);
                    }

                    // Проверка дали потребителят е създател или член на групата
                    bool isGroupCreator = userGroup.CreatorId == user.Id;
                    bool isGroupMember = userGroup.Members.Any(m => m.Id == user.Id);

                    if (!isGroupCreator && !isGroupMember)
                    {
                        ModelState.AddModelError("", "You do not have permission to post on behalf of the group.");
                        return View(model);
                    }

                    post.GroupId = userGroup.Id;
                }
                else
                {
                    post.UserId = user.Id;
                }

                _context.Posts.Add(post);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving new post to database.");
                    ModelState.AddModelError("", "We were unable to create the post.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "The publication was successfully created.";
                return RedirectToAction("All");
            }

            // Ако моделът не е валиден, повторно зареждане на информация за групата
            var currentUser = await _userManager.GetUserAsync(User);
            var currentGroup = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Members.Any(m => m.Id == currentUser.Id) || g.CreatorId == currentUser.Id);

            if (currentGroup != null)
            {
                model.IsGroupPost = true;
                model.UserGroupName = currentGroup.GroupName;
                model.IsGroupCreator = currentGroup.CreatorId == currentUser.Id;
            }
            else
            {
                model.IsGroupPost = false;
            }

            return View(model);
        }

        // GET: Feed/Edit/{id} - Display form to edit a post
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User != null && post.User.Id != currentUser.Id)
            {
                return Forbid();
            }
            else if (post.Group != null && post.Group.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            var model = new CreatePostViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                IsGroupPost = post.Group != null
            };

            if (post.Group != null)
            {
                var userGroup = await _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Id == post.GroupId);

                model.UserGroupName = userGroup?.GroupName;
                model.IsGroupCreator = userGroup?.CreatorId == currentUser.Id;
            }

            return View(model);
        }

        // POST: Feed/Edit/{id} - Handle post edit form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CreatePostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post {PostId}.", id);
                ModelState.AddModelError("", "We were unable to update the post.");
                return View(model);
            }

            TempData["SuccessMessage"] = "The post was successfully edited.";
            return RedirectToAction("All");
        }

        // GET: Feed/Delete/{id} - Display confirmation form to delete a post
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User != null && post.User.Id != currentUser.Id)
            {
                return Forbid();
            }
            else if (post.Group != null && post.Group.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            var model = new DeleteViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Publisher = post.User != null ? post.User.UserName : post.Group.GroupName
            };

            return View(model);
        }

        // POST: Feed/DeleteConfirmed - Handle post deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DeleteViewModel model)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Проверка дали текущият потребител е създател на публикацията
            if (post.User != null && post.User.Id != currentUser.Id)
            {
                return Forbid();
            }
            else if (post.Group != null && post.Group.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            _context.Posts.Remove(post);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}.", model.Id);
                TempData["ErrorMessage"] = "We were unable to delete the post.";
                return View(model);
            }

            TempData["SuccessMessage"] = "The post was successfully deleted.";
            return RedirectToAction("All");
        }

        // GET: Feed/UserDetails/{id} - Redirect to UserController Details
        [HttpGet]
        public IActionResult UserDetails(Guid id)
        {
            return RedirectToAction("Details", "User", new { id });
        }
    }
}
