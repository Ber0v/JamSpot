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
                .AnyAsync(g => g.CreatorId == user.Id);

            var isMemberOfGroup = await context.Groups
                .AnyAsync(g => g.Members.Any(m => m.Id == user.Id));

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
            ViewBag.IsMemberOfGroup = isMemberOfGroup;

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

                // Проверка дали потребителят вече е създал група
                var userHasGroup = await context.Groups.AnyAsync(g => g.CreatorId == user.Id);

                // Проверка дали потребителят е член на друга група
                var isMemberOfGroup = await context.Groups
                    .AnyAsync(g => g.Members.Any(m => m.Id == user.Id));

                if (userHasGroup || isMemberOfGroup)
                {
                    ModelState.AddModelError("", "Не можете да създадете група, ако вече сте създали група или сте член на група.");
                    return View(model);
                }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(Guid groupId, Guid userId)
        {
            var group = await context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Проверка дали текущият потребител е създателят на групата
            if (group.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            var userToAdd = await _userManager.FindByIdAsync(userId.ToString());

            if (userToAdd == null)
            {
                ModelState.AddModelError("", "Потребителят не е намерен.");
                return RedirectToAction("Details", new { id = groupId });
            }

            // Проверка дали потребителят вече е член на групата
            if (group.Members.Any(m => m.Id == userToAdd.Id))
            {
                ModelState.AddModelError("", "Потребителят вече е член на групата.");
                return RedirectToAction("Details", new { id = groupId });
            }

            // Проверка дали потребителят вече е създал група или е член на друга група
            var userHasGroup = await context.Groups.AnyAsync(g => g.CreatorId == userToAdd.Id);
            var isMemberOfGroup = await context.Groups
                .AnyAsync(g => g.Members.Any(m => m.Id == userToAdd.Id));

            if (userHasGroup || isMemberOfGroup)
            {
                ModelState.AddModelError("", "Потребителят не може да бъде добавен, защото вече е създател или член на друга група.");
                return RedirectToAction("Details", new { id = groupId });
            }

            // Добавяне на потребителя към групата
            group.Members.Add(userToAdd);
            await context.SaveChangesAsync();

            // Добавяне на потребителя в ролята "GroupMember", ако не е вече в нея
            if (!await _userManager.IsInRoleAsync(userToAdd, "GroupMember"))
            {
                await _userManager.AddToRoleAsync(userToAdd, "GroupMember");
            }

            return RedirectToAction("Details", new { id = groupId });
        }


        // GET: /Group/Details - Показва групата на текущия потребител
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var model = await context.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Members)
                .Include(g => g.Creator)
                .AsNoTracking()
                .Select(g => new GroupDetailsViewModel()
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    Logo = g.Logo,
                    Description = g.Description,
                    Genre = g.Genre,
                    Creator = g.Creator.UserName,
                    CreatorId = g.CreatorId,
                    Members = g.Members.Select(m => new GroupMemberViewModel
                    {
                        UserId = m.Id,
                        UserName = m.UserName,
                        Instrument = m.Instrument
                    }).ToList(),
                    IsGroupAdmin = g.CreatorId == currentUser.Id
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            // Проверка дали текущият потребител е създателят на групата
            var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (currentUserId == model.CreatorId.ToString())
            {
                // Извличане на потребители, които могат да бъдат добавени
                var availableUsers = await _userManager.Users
                    .Where(u => !context.Groups.Any(g => g.CreatorId == u.Id) &&
                                !context.Groups.Any(g => g.Members.Any(m => m.Id == u.Id)) &&
                                u.Id != model.CreatorId) // Изключваме създателя
                    .Select(u => new UserSelectionViewModel
                    {
                        UserId = u.Id,
                        UserName = u.UserName,
                        Instrument = u.Instrument
                    })
                    .ToListAsync();

                ViewBag.AvailableUsers = availableUsers;
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId)
        {
            var group = await context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Check if the current user is the creator of the group
            if (group.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            var userToRemove = await _userManager.FindByIdAsync(userId.ToString());

            if (userToRemove == null)
            {
                ModelState.AddModelError("", "Потребителят не е намерен.");
                return RedirectToAction("Details", new { id = groupId });
            }

            // Prevent removing the group creator
            if (userToRemove.Id == group.CreatorId)
            {
                ModelState.AddModelError("", "Не можете да премахнете създателя на групата.");
                return RedirectToAction("Details", new { id = groupId });
            }

            // Check if the user is a member of the group
            if (!group.Members.Any(m => m.Id == userToRemove.Id))
            {
                ModelState.AddModelError("", "Потребителят не е член на групата.");
                return RedirectToAction("Details", new { id = groupId });
            }

            // Remove the user from the group
            group.Members.Remove(userToRemove);
            await context.SaveChangesAsync();

            // Remove the "GroupMember" role if the user is no longer part of any group
            var isStillMember = await context.Groups.AnyAsync(g => g.Members.Any(m => m.Id == userToRemove.Id));
            var isCreator = await context.Groups.AnyAsync(g => g.CreatorId == userToRemove.Id);

            if (!isStillMember && !isCreator)
            {
                if (await _userManager.IsInRoleAsync(userToRemove, "GroupMember"))
                {
                    await _userManager.RemoveFromRoleAsync(userToRemove, "GroupMember");
                }
            }

            return RedirectToAction("Details", new { id = groupId });
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
