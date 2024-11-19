using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    using Microsoft.AspNetCore.Identity;

    public class MusicController : Controller
    {
        private readonly JamSpotDbContext context;
        private readonly UserManager<User> _userManager;

        public MusicController(JamSpotDbContext _context, UserManager<User> userManager)
        {
            context = _context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> All()
        {
            return View();
        }
    }
}
