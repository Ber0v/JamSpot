using JamSpotApp.Data;
using JamSpotApp.Models;
using JamSpotApp.Models.Event;
using JamSpotApp.Models.feed;
using JamSpotApp.Models.Home;
using JamSpotApp.Models.Message;
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
            _logger.LogInformation("Index action invoked.");

            try
            {
                // Fetch admin messages
                var adminMessages = await _context.Messages
                    .Where(m => m.IsFromAdmin && m.Pinned)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new MessageViewModel
                    {
                        Id = m.Id,
                        Username = m.Username.UserName,
                        Title = m.Title,
                        Content = m.Content,
                        CreatedAt = m.CreatedAt,
                        IsFromAdmin = m.IsFromAdmin,
                        Pinned = m.Pinned
                    })
                    .ToListAsync();

                ViewBag.AdminMessages = adminMessages;

                // Retrieve upcoming events with AsNoTracking for performance
                var events = await _context.Events
                    .AsNoTracking()
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
                        OrganizerId = e.Organizer.Id,
                        Location = e.Location,
                        Date = e.Date.ToString("dd.MM.yyyy"),
                        Hour = e.Hour.ToString("HH\\:mm"),
                    })
                    .ToListAsync();

                // Retrieve latest posts with AsNoTracking
                var posts = await _context.Posts
                    .AsNoTracking()
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
                        PublisherId = p.User != null ? p.User.Id : p.GroupId,
                        IsGroupPost = p.Group != null,
                        CreatedDate = p.CreatedDate.ToString("yyyy-MM-dd"),
                    })
                    .ToListAsync();

                // Combine into the IndexViewModel
                var model = new IndexViewModel
                {
                    Events = events ?? new List<EventViewModel>(),
                    Posts = posts ?? new List<FeedViewModel>()
                };

                _logger.LogInformation("Index action completed successfully.");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the home page.");
                // Optionally, redirect to an error page or display a user-friendly message
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Search query was empty or null.");
                return BadRequest("Query cannot be empty.");
            }

            _logger.LogInformation("Search action invoked with query: {Query}", query);

            try
            {
                // Limit the number of results to enhance performance
                int maxResults = 10;

                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => EF.Functions.Like(u.UserName, $"%{query}%"))
                    .Select(u => new UserResultViewModel
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        AvatarUrl = u.ProfilePicture
                    })
                    .Take(maxResults)
                    .ToListAsync();

                var groups = await _context.Groups
                    .AsNoTracking()
                    .Where(g => EF.Functions.Like(g.GroupName, $"%{query}%"))
                    .Select(g => new GroupResultViewModel
                    {
                        Id = g.Id,
                        GroupName = g.GroupName,
                        LogoUrl = g.Logo
                    })
                    .Take(maxResults)
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

            _logger.LogInformation("AutoComplete action invoked with query: {Query}", query);

            try
            {
                // Limit the number of results to enhance performance
                int maxResults = 5;

                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => EF.Functions.Like(u.UserName, $"%{query}%"))
                    .Select(u => new { u.Id, u.UserName, avatarUrl = u.ProfilePicture })
                    .Take(maxResults)
                    .ToListAsync();

                var groups = await _context.Groups
                    .AsNoTracking()
                    .Where(g => EF.Functions.Like(g.GroupName, $"%{query}%"))
                    .Select(g => new { g.Id, g.GroupName, logoUrl = g.Logo })
                    .Take(maxResults)
                    .ToListAsync();

                _logger.LogInformation("AutoComplete search completed successfully for query: {Query}", query);

                return Json(new { users = users, groups = groups });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during autocomplete search with query: {Query}", query);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Error action invoked.");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

