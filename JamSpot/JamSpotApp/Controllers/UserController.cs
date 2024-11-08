using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    public class UserController : Controller
    {
        public IActionResult All()
        {
            return View();
        }
    }
}
