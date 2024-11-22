using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBaseController : Controller
    {
        public IActionResult ManageUsers()
        {
            return View();
        }
    }
}
