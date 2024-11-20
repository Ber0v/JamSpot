using JamSpotApp.Data;
using JamSpotApp.Models;
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

        [HttpGet]
        public async Task<IActionResult> AutoComplete(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return Json(new { users = new List<object>(), groups = new List<object>() });
            }

            var users = await _context.Users
                .Where(u => EF.Functions.Like(u.UserName, $"%{query}%"))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            var groups = await _context.Groups
                .Where(g => EF.Functions.Like(g.GroupName, $"%{query}%"))
                .Select(g => new { g.Id, g.GroupName })
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
