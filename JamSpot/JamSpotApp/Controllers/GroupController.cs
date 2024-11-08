using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Controllers
{
    using JamSpotApp.Models.Group;
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

        // GET: /Group/MyGroup - Показва групата на текущия потребител
        [HttpGet]
        public async Task<IActionResult> MyGroup()
        {
            var user = await _userManager.GetUserAsync(User);

            var group = await context.Groups
                .Include(g => g.Creator)
                .FirstOrDefaultAsync(g => g.Creator.Id == user.Id);

            if (group == null)
            {
                // Потребителят няма създадена група
                return RedirectToAction("CreateGroup");
            }

            var isOwner = group.Creator.Id == user.Id;

            var model = new GroupDetailsViewModel
            {
                Id = group.Id,
                GroupName = group.GroupName,
                Logo = group.Logo,
                Description = group.Description,
                Genre = group.Genre,
                Creator = group.Creator.UserName,
                IsOwner = isOwner,
            };

            return View(model);
        }

        // GET: /Group/Edit/{id} - Извежда форма за редактиране на съществуваща група
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var group = await context.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Creator)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (group == null)
            {
                return NotFound();
            }

            var model = new CreateGroupViewModel()
            {
                GroupName = group.GroupName,
                Description = group.Description,
                Genre = group.Genre,
                ExistingLogoPath = group.Logo
            };

            return View(model);
        }

        // POST: /Group/Edit/{id} - Обработва редакцията на групата
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, CreateGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var group = await context.Groups.FindAsync(id);

            if (group == null)
            {
                return NotFound();
            }

            group.GroupName = model.GroupName;
            group.Description = model.Description;
            group.Genre = model.Genre;

            // Проверка дали има качено ново лого
            if (model.Logo != null)
            {
                // Изтриваме старото лого от файловата система, ако съществува
                if (!string.IsNullOrEmpty(group.Logo) && group.Logo != DefaultLogo())
                {
                    var oldLogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", group.Logo.TrimStart('/'));
                    if (System.IO.File.Exists(oldLogoPath))
                    {
                        System.IO.File.Delete(oldLogoPath);
                    }
                }

                // Качваме новото лого и го записваме
                group.Logo = UploadLogo(model.Logo);
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(All));
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
