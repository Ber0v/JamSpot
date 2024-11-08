using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    using JamSpotApp.Models.User;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

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
                UserName = user.UserName,
                UserBio = user.UserBio,
                Instrument = user.Instrument
            };

            return View(model);
        }

        // GET: /User/Edit/{id} - Извежда форма за редактиране на съществуваща user
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await context.Users
                .Where(g => g.Id == id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var model = new EditUserViewModel()
            {
                ExistingPicturePath = user.ProfilePicture,
                UserName = user.UserName,
                UserBio = user.UserBio,
                Instrument = user.Instrument
            };

            return View(model);
        }

        // POST: /User/Edit/{id} - Обработва редакцията на user
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await context.Users.FindAsync(id);

            user.UserName = model.UserName;
            user.UserBio = model.UserBio;
            user.Instrument = model.Instrument;

            // Проверка дали има качено ново лого
            if (model.ProfilePicture != null)
            {
                // Изтриваме старото лого от файловата система, ако съществува
                if (!string.IsNullOrEmpty(user.ProfilePicture) && user.ProfilePicture != DefaultLogo())
                {
                    var oldLogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldLogoPath))
                    {
                        System.IO.File.Delete(oldLogoPath);
                    }
                }

                // Качваме новото лого и го записваме
                user.ProfilePicture = UploadLogo(model.ProfilePicture);
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
