using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    public class GroupController : Controller
    {
        public IActionResult All()
        {
            return View();
        }
    }
}
