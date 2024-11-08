using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    using JamSpotApp.Models.User;
    using Microsoft.AspNetCore.Identity;
    public class UserController : Controller
    {
        private readonly JamSpotDbContext context;
        private readonly UserManager<User> _userManager;

        public UserController(JamSpotDbContext _context, UserManager<User> userManager)
        {
            context = _context;
            _userManager = userManager;
        }
        public async Task<IActionResult> All()
        {
            var user = await _userManager.GetUserAsync(User);

            var model = new UserViewModel
            {
                Id = user.Id,
                ProfilePicture = user.ProfilePicture,
                UserName = user.ProfilePicture,
                UserBio = user.UserBio,
                Instrument = user.Instrument
            };

            return View(model);
        }
    }
}
