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
            var user = await _userManager.GetUserAsync(User);

            var userHasGroup = await context.Groups
                .AnyAsync(g => g.Creator.Id == user.Id);

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

            ViewBag.UserHasGroup = userHasGroup;

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

                // Проверка и добавяне на потребителя в ролята "GroupAdmin"
                if (!await _userManager.IsInRoleAsync(user, "GroupAdmin"))
                {
                    var result = await _userManager.AddToRoleAsync(user, "GroupAdmin");

                    if (!result.Succeeded)
                    {
                        ModelState.AddModelError("", "Не успяхме да добавим потребителя в ролята 'GroupAdmin'.");
                        return View(model);
                    }
                }

                return RedirectToAction("Details", new { id = group.Id });
            }

            return View(model);
        }

        // GET: /Group/Details - Показва групата на текущия потребител
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var model = await context.Groups
                .Where(g => g.Id == id)
                .AsNoTracking()
                .Select(g => new GroupDetailsViewModel()
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    Logo = g.Logo,
                    Description = g.Description,
                    Genre = g.Genre,
                    Creator = g.Creator.UserName,
                })
                .FirstOrDefaultAsync();

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

            // Проверка дали текущият потребител е създателят
            var user = await _userManager.GetUserAsync(User);
            if (group.Creator.Id != user.Id)
            {
                return Forbid(); // или return Unauthorized();
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

            var group = await context.Groups
                .Include(g => g.Creator)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създателят
            if (group.Creator.Id != user.Id)
            {
                return Forbid(); // или return Unauthorized();
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
            return RedirectToAction("Details", new { id = group.Id });

        }

        // GET: /Group/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var group = await context.Groups
                .Include(g => g.Creator)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създателят
            if (group.Creator.Id != user.Id)
            {
                return Forbid(); // или return Unauthorized();
            }

            var model = new DeleteGroupViewModel()
            {
                Id = group.Id,
                GroupName = group.GroupName,
                Creator = group.Creator.UserName
            };

            return View(model);
        }

        // POST: /Group/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DeleteGroupViewModel model)
        {
            var groupToDelete = await context.Groups
                .Include(g => g.Creator)
                .FirstOrDefaultAsync(g => g.Id == model.Id);

            if (groupToDelete == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създателят
            if (groupToDelete.Creator.Id != user.Id)
            {
                return Forbid(); // или return Unauthorized();
            }

            context.Groups.Remove(groupToDelete);
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
