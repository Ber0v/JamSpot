using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly JamSpotDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<UserController> _logger;

        public UserController(
            JamSpotDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<UserController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /User/All/{id?}
        [HttpGet]
        public async Task<IActionResult> All(Guid? id)
        {
            UserViewModel model;

            if (id.HasValue)
            {
                // Display a specific user by ID
                model = await _context.Users
                    .Where(u => u.Id == id.Value)
                    .Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        ProfilePicture = u.ProfilePicture ?? DefaultLogo(),
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
                // Display the current user's profile
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                model = new UserViewModel
                {
                    Id = user.Id,
                    ProfilePicture = user.ProfilePicture ?? DefaultLogo(),
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

        // GET: /User/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Authorization: Only the user themselves can edit
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id != Guid.Parse(currentUserId))
            {
                return Forbid();
            }

            var model = new EditUserViewModel
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

        // POST: /User/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Authorization: Only the user themselves can edit
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id != Guid.Parse(currentUserId))
            {
                return Forbid();
            }

            user.UserName = model.UserName;
            user.NormalizedUserName = _userManager.NormalizeName(model.UserName);
            user.UserBio = model.UserBio;
            user.InstagramUrl = string.IsNullOrWhiteSpace(model.InstagramUrl) ? null : model.InstagramUrl;
            user.FacebookUrl = string.IsNullOrWhiteSpace(model.FacebookUrl) ? null : model.FacebookUrl;
            user.Instrument = model.Instrument;

            if (model.ProfilePicture != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(user.ProfilePicture) && user.ProfilePicture != DefaultLogo())
                    {
                        var oldLogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
                        if (System.IO.File.Exists(oldLogoPath))
                        {
                            System.IO.File.Delete(oldLogoPath);
                        }
                    }

                    user.ProfilePicture = await UploadLogoAsync(model.ProfilePicture);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Image upload failed.");
                    _logger.LogError(ex, "Error uploading image for user {UserId}.", user.Id);
                    return View(model);
                }
            }

            try
            {
                await _userManager.UpdateSecurityStampAsync(user);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Error updating the profile.");
                return View(model);
            }

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            await _signInManager.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("User {UserId} edited successfully.", user.Id);

            return RedirectToAction(nameof(All));
        }

        // GET: /User/Delete/{id}
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id.HasValue)
            {
                var model = await _context.Users
                    .Where(p => p.Id == id.Value)
                    .AsNoTracking()
                    .Select(p => new DeleteUserViewModel
                    {
                        Id = p.Id,
                        UserName = p.UserName,
                    })
                    .FirstOrDefaultAsync();

                if (model == null)
                {
                    return NotFound();
                }

                // Authorization: Only the user themselves can delete
                var currentUserId = _userManager.GetUserId(User);
                if (model.Id != Guid.Parse(currentUserId))
                {
                    return Forbid();
                }

                return View(model);
            }
            else
            {
                // User is signed out, show deletion confirmation message
                return View(null);
            }
        }

        // POST: /User/DeleteConfirmed
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DeleteUserViewModel model)
        {
            var user = await _context.Users.FindAsync(model.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Authorization: Only the user themselves can delete
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id != Guid.Parse(currentUserId))
            {
                return Forbid();
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await _userManager.UpdateSecurityStampAsync(user);
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                _logger.LogInformation("User {UserId} deleted successfully.", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Error deleting the user.");
                return View(model);
            }

            return RedirectToAction("Delete", "User");
        }

        // Helper method to upload profile pictures
        private async Task<string> UploadLogoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Unsupported image format.");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                throw new InvalidOperationException("File size exceeds the limit of 2MB.");
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/uploads/" + uniqueFileName;
        }

        // Returns the default profile picture path
        private string DefaultLogo()
        {
            return "/Pictures/DefaultUser.png";
        }
    }
}
