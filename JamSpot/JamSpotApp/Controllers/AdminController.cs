using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    [Area("Admin")]
    public class AdminController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken] // Задължителен за защита от CSRF
        public IActionResult ToggleAdminMode(bool isAdminModeEnabled)
        {
            if (isAdminModeEnabled)
            {
                Response.Cookies.Append("IsAdminMode", "true", new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.Now.AddHours(1) });
            }
            else
            {
                Response.Cookies.Delete("IsAdminMode");
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }
    }
}
