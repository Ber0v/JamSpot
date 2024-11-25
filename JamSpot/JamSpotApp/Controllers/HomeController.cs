using JamSpotApp.Data;
using JamSpotApp.Models;
using JamSpotApp.Models.Event;
using JamSpotApp.Models.feed;
using JamSpotApp.Models.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JamSpotApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly JamSpotDbContext _context;

        public HomeController(ILogger<HomeController> logger, JamSpotDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Вземане на събития
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.Date >= DateTime.Today)
                .OrderBy(e => e.Date)
                .Take(3)
                .Select(e => new EventViewModel()
                {
                    Id = e.Id,
                    EventName = e.EventName,
                    EventDescription = e.EventDescription,
                    Price = e.Price,
                    Organizer = e.Organizer.UserName,
                    Location = e.Location,
                    Date = e.Date.ToString("dd.MM.yyyy"),
                    Hour = e.Hour.ToString("HH\\:mm"),
                })
                .ToListAsync();

            // Вземане на публикации
            var posts = await _context.Posts
                .Include(p => p.User)
                .Take(2)
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

            // Комбиниране в модел
            var model = new IndexViewModel
            {
                Events = events ?? new List<EventViewModel>(),
                Posts = posts ?? new List<FeedViewModel>()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Search query was empty or null.");
                return BadRequest("Query cannot be empty.");
            }

            try
            {
                var users = await _context.Users
                    .Where(u => EF.Functions.Like(u.UserName, $"%{query}%"))
                    .Select(u => new UserResultViewModel
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        AvatarUrl = u.ProfilePicture
                    })
                    .ToListAsync();

                var groups = await _context.Groups
                    .Where(g => EF.Functions.Like(g.GroupName, $"%{query}%"))
                    .Select(g => new GroupResultViewModel
                    {
                        Id = g.Id,
                        GroupName = g.GroupName,
                        LogoUrl = g.Logo
                    })
                    .ToListAsync();

                var viewModel = new SearchResultsViewModel
                {
                    Users = users,
                    Groups = groups
                };

                _logger.LogInformation("Search completed successfully for query: {Query}", query);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during search with query: {Query}", query);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AutoComplete(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return Json(new { users = new List<object>(), groups = new List<object>() });
            }

            var users = await _context.Users
                .Where(u => EF.Functions.Like(u.UserName, $"%{query}%"))
                .Select(u => new { u.Id, u.UserName, avatarUrl = u.ProfilePicture })
                .ToListAsync();

            var groups = await _context.Groups
                .Where(g => EF.Functions.Like(g.GroupName, $"%{query}%"))
                .Select(g => new { g.Id, g.GroupName, logoUrl = g.Logo })
                .ToListAsync();

            return Json(new { users = users, groups = groups });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
