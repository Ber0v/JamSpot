using JamSpotApp.Data;
using JamSpotApp.Models;
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

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return View(new SearchResultsViewModel
                {
                    Users = new List<UserResultViewModel>(),
                    Groups = new List<GroupResultViewModel>()
                });
            }

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

            return View(viewModel);
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
