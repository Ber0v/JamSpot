using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Controllers
{
    using JamSpotApp.Models;
    using Microsoft.AspNetCore.Identity;
    public class GroupController : Controller
    {
        private readonly JamSpotDbContext context;
        private readonly UserManager<User> _userManager;

        public GroupController(JamSpotDbContext _context, UserManager<User> userManager)
        {
            context = _context;
            _userManager = userManager;
        }

        // GET: /Group/All - Display all group
        public async Task<IActionResult> All()
        {
            var model = await context.Groups
                .Include(p => p.Creator)
                .Select(p => new GroupViewModel()
                {
                    Id = p.Id,
                    GroupName = p.GroupName,
                    Logo = p.Logo != null ? p.Logo : null,
                    Genre = p.Genre,
                })
                .AsNoTracking()
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateGroup()
        {
            var model = new CreateGroupViewModel();
            return View(model);
        }

        // POST: /Feed/CreatePost - Handle form submission for musician search posts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CreateGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                var group = new Group
                {
                    Logo = model.Logo != null ? UploadLogo(model.Logo) : DefaultLogo(),
                    GroupName = model.GroupName,
                    Description = model.Description,
                    Genre = model.Genre,
                    Creator = user
                };

                context.Groups.Add(group);
                await context.SaveChangesAsync();

                return RedirectToAction("All");
            }

            return View(model);
        }

        private string UploadLogo(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            // Генерираме уникално име за файла
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;

            // Определяме директорията, където ще съхраняваме файловете
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

            // Създаваме директорията, ако не съществува
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Пълния път на файла
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Качваме файла в директорията
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            // Връщаме относителния път, за да може да бъде използван в HTML
            return "/uploads/" + uniqueFileName;
        }

        private string DefaultLogo()
        {
            return "https://static.vecteezy.com/system/resources/thumbnails/046/300/546/small/avatar-user-profile-person-icon-gender-neutral-silhouette-profile-picture-suitable-for-social-media-profiles-icons-screensavers-free-png.png";
        }
    }
}
