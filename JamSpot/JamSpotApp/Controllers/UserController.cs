using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace JamSpotApp.Controllers
{
    using JamSpotApp.Models.User;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public class UserController : Controller
    {
        private readonly JamSpotDbContext context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserController(JamSpotDbContext _context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            context = _context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> All(Guid? id)
        {
            UserViewModel model;

            if (id.HasValue)
            {
                // Показва конкретен потребител по ID
                model = await context.Users
                    .Where(u => u.Id == id.Value)
                    .Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        ProfilePicture = u.ProfilePicture,
                        UserName = u.UserName,
                        UserBio = u.UserBio,
                        InstagramUrl = u.InstagramUrl,
                        FacebookUrl = u.FacebookUrl,
                        Instrument = u.Instrument
                    })
                    .FirstOrDefaultAsync();
            }
            else
            {
                // Логика за общ преглед, ако ID не е подадено
                var user = await _userManager.GetUserAsync(User);
                model = new UserViewModel
                {
                    Id = user.Id,
                    ProfilePicture = user.ProfilePicture,
                    UserName = user.UserName,
                    UserBio = user.UserBio,
                    InstagramUrl = user.InstagramUrl,
                    FacebookUrl = user.FacebookUrl,
                    Instrument = user.Instrument
                };
            }

            if (model == null)
            {
                return NotFound();
            }

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
                Id = id,
                ExistingPicturePath = user.ProfilePicture,
                UserName = user.UserName,
                UserBio = user.UserBio,
                InstagramUrl = user.InstagramUrl,
                FacebookUrl = user.FacebookUrl,
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
            user.NormalizedUserName = _userManager.NormalizeName(model.UserName);
            user.UserBio = model.UserBio;
            user.InstagramUrl = string.IsNullOrWhiteSpace(model.InstagramUrl) ? null : model.InstagramUrl;
            user.FacebookUrl = string.IsNullOrWhiteSpace(model.FacebookUrl) ? null : model.FacebookUrl;
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

            // Обновяване на SecurityStamp
            await _userManager.UpdateSecurityStampAsync(user);

            await context.SaveChangesAsync();

            // Изход и вход с актуализирани данни
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var model = await context.Users
                .Where(p => p.Id == id)
                .AsNoTracking()
                .Select(p => new DeleteUserViewModel()
                {
                    Id = p.Id,
                    UserName = p.UserName,
                })
                .FirstOrDefaultAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(DeleteUserViewModel model)
        {
            var user = await context.Users.FindAsync(model.Id);

            if (user == null)
            {
                return NotFound();
            }

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            await _userManager.UpdateSecurityStampAsync(user);
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            return RedirectToAction("Delete");
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
