using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    public class EventController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
