using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/404")]
        public IActionResult NotFoundError()
        {
            return View("404");
        }

        [Route("Error")]
        public IActionResult ServerError()
        {
            return View("500");
        }
    }
}
