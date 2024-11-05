using JamSpotApp.Data;
using Microsoft.AspNetCore.Mvc;

namespace ArtJamWebApp.Controllers
{
    public class FeedController : Controller
    {
        private readonly JamSpotDbContext _context;

        public FeedController(JamSpotDbContext context)
        {
            _context = context;
        }

        // GET: /Feed/Index - Display all posts (Records, Events, MusicianSearch posts)
        public async Task<IActionResult> Index()
        {
            return View();
        }

        // GET: /Feed/CreatePost - Display form for creating a musician search post
        public IActionResult CreatePost()
        {
            return View();
        }

        // POST: /Feed/CreatePost - Handle form submission for musician search posts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            return RedirectToAction("Index");
        }
    }
}
