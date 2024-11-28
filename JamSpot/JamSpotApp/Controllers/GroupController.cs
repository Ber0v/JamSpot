using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.Group;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly JamSpotDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<GroupController> _logger;

        public GroupController(JamSpotDbContext context, UserManager<User> userManager, ILogger<GroupController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Group/All - Display all groups with pagination
        public async Task<IActionResult> All(int pageNumber = 1)
        {
            int pageSize = 10; // Number of groups per page

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Redirect to login if not authenticated
            }

            var userHasGroup = await _context.Groups.AnyAsync(g => g.CreatorId == user.Id);
            var isMemberOfGroup = await _context.Groups.AnyAsync(g => g.Members.Any(m => m.Id == user.Id));

            // Total number of groups
            var totalGroups = await _context.Groups.CountAsync();

            // Calculate total pages
            int totalPages = (int)Math.Ceiling(totalGroups / (double)pageSize);

            // Retrieve groups for the current page
            var groups = await _context.Groups
                .Include(p => p.Creator)
                .OrderBy(p => p.GroupName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new GroupViewModel()
                {
                    Id = p.Id,
                    GroupName = p.GroupName,
                    Logo = p.Logo ?? DefaultLogo(),
                    Genre = p.Genre,
                })
                .AsNoTracking()
                .ToListAsync();

            var model = new GroupListViewModel
            {
                Groups = groups,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                UserHasGroup = userHasGroup,
                IsMemberOfGroup = isMemberOfGroup
            };

            return View(model);
        }

        // GET: /Group/CreateGroup - Display form to create a new group
        [HttpGet]
        public IActionResult CreateGroup()
        {
            var model = new CreateGroupViewModel();
            return View(model);
        }

        // POST: /Group/CreateGroup - Handle form submission to create a new group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CreateGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge(); // Redirect to login if not authenticated
                }

                // Check if the user has already created a group or is a member of another group
                var userHasGroup = await _context.Groups.AnyAsync(g => g.CreatorId == user.Id);
                var isMemberOfGroup = await _context.Groups.AnyAsync(g => g.Members.Any(m => m.Id == user.Id));

                if (userHasGroup || isMemberOfGroup)
                {
                    ModelState.AddModelError("", "You cannot create a group if you have already created a group or are a member of a group.");
                    return View(model);
                }

                string logoPath;
                try
                {
                    logoPath = model.Logo != null ? await UploadLogoAsync(model.Logo) : DefaultLogo();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading logo for group creation.");
                    ModelState.AddModelError("", "We were unable to upload the group logo.");
                    return View(model);
                }

                var group = new Group
                {
                    Logo = logoPath,
                    GroupName = model.GroupName,
                    Description = model.Description,
                    Genre = model.Genre,
                    CreatorId = user.Id,
                    Creator = user
                };

                _context.Groups.Add(group);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving new group to database.");
                    ModelState.AddModelError("", "We were unable to create the group.");
                    return View(model);
                }

                // Add user to "GroupAdmin" role if not already in it
                if (!await _userManager.IsInRoleAsync(user, "GroupAdmin"))
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, "GroupAdmin");
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError("Failed to add user {UserId} to role 'GroupAdmin'.", user.Id);
                        ModelState.AddModelError("", "We were unable to add the user to the role.'GroupAdmin'.");
                        return View(model);
                    }
                }

                TempData["SuccessMessage"] = "The group was successfully created.";
                return RedirectToAction("Details", new { id = group.Id });
            }

            return View(model);
        }

        // POST: /Group/AddMember - Add a member to a group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(Guid groupId, Guid userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Check if the current user is the creator of the group
            if (group.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            var userToAdd = await _userManager.FindByIdAsync(userId.ToString());
            if (userToAdd == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Check if the user is already a member of the group
            if (group.Members.Any(m => m.Id == userToAdd.Id))
            {
                TempData["ErrorMessage"] = "The user is already a member of the group.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Check if the user has already created a group or is a member of another group
            var userHasGroup = await _context.Groups.AnyAsync(g => g.CreatorId == userToAdd.Id);
            var isMemberOfGroup = await _context.Groups.AnyAsync(g => g.Members.Any(m => m.Id == userToAdd.Id));

            if (userHasGroup || isMemberOfGroup)
            {
                TempData["ErrorMessage"] = "The user cannot be added because they are already a creator or member of another group.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Add the user to the group
            group.Members.Add(userToAdd);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member {UserId} to group {GroupId}.", userId, groupId);
                TempData["ErrorMessage"] = "We were unable to add the user to the group.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Add the user to "GroupMember" role if not already in it
            if (!await _userManager.IsInRoleAsync(userToAdd, "GroupMember"))
            {
                var roleResult = await _userManager.AddToRoleAsync(userToAdd, "GroupMember");
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to add user {UserId} to role 'GroupMember'.", userToAdd.Id);
                    TempData["ErrorMessage"] = "We were unable to add the user to the role. 'GroupMember'.";
                    return RedirectToAction("Details", new { id = groupId });
                }
            }

            TempData["SuccessMessage"] = "The user was successfully added to the group.";
            return RedirectToAction("Details", new { id = groupId });
        }

        // GET: /Group/Details/{id} - Display group details
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var model = await _context.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Members)
                .Include(g => g.Creator)
                .AsNoTracking()
                .Select(g => new GroupDetailsViewModel()
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    Logo = g.Logo ?? DefaultLogo(),
                    Description = g.Description,
                    InstagramUrl = g.InstagramUrl,
                    FacebookUrl = g.FacebookUrl,
                    Genre = g.Genre,
                    Creator = g.Creator.UserName,
                    CreatorId = g.CreatorId,
                    Members = g.Members.Select(m => new GroupMemberViewModel
                    {
                        UserId = m.Id,
                        UserName = m.UserName,
                        Instrument = m.Instrument,
                        ProfilePicture = m.ProfilePicture ?? DefaultProfilePicture()
                    }).ToList(),
                    IsGroupAdmin = g.CreatorId == currentUser.Id
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            if (model.IsGroupAdmin)
            {
                // Retrieve users that can be added to the group
                var availableUsers = await _userManager.Users
                    .Where(u => !_context.Groups.Any(g => g.CreatorId == u.Id) &&
                                !_context.Groups.Any(g => g.Members.Any(m => m.Id == u.Id)) &&
                                u.Id != model.CreatorId)
                    .Select(u => new UserSelectionViewModel
                    {
                        UserId = u.Id,
                        UserName = u.UserName,
                        Instrument = u.Instrument,
                        ProfilePicture = u.ProfilePicture ?? DefaultProfilePicture()
                    })
                    .ToListAsync();

                model.AvailableUsers = availableUsers;
            }

            return View(model);
        }

        // GET: /Group/Edit/{id} - Display form to edit an existing group
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var group = await _context.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Creator)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (group == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Check if the current user is the creator of the group
            if (group.CreatorId != user.Id)
            {
                return Forbid();
            }

            var model = new CreateGroupViewModel()
            {
                Id = group.Id,
                GroupName = group.GroupName,
                Description = group.Description,
                InstagramUrl = group.InstagramUrl,
                FacebookUrl = group.FacebookUrl,
                Genre = group.Genre,
                ExistingLogoPath = group.Logo
            };

            return View(model);
        }

        // POST: /Group/Edit/{id} - Handle group edit form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CreateGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var group = await _context.Groups
                .Include(g => g.Creator)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Check if the current user is the creator of the group
            if (group.CreatorId != user.Id)
            {
                return Forbid();
            }

            group.GroupName = model.GroupName;
            group.Description = model.Description;
            group.InstagramUrl = string.IsNullOrWhiteSpace(model.InstagramUrl) ? null : model.InstagramUrl;
            group.FacebookUrl = string.IsNullOrWhiteSpace(model.FacebookUrl) ? null : model.FacebookUrl;
            group.Genre = model.Genre;

            // Check if a new logo has been uploaded
            if (model.Logo != null)
            {
                try
                {
                    // Delete the old logo if it's not the default
                    if (!string.IsNullOrEmpty(group.Logo) && !group.Logo.Equals(DefaultLogo(), StringComparison.OrdinalIgnoreCase))
                    {
                        var oldLogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", group.Logo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(oldLogoPath))
                        {
                            System.IO.File.Delete(oldLogoPath);
                        }
                    }

                    // Upload the new logo
                    group.Logo = await UploadLogoAsync(model.Logo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading new logo for group {GroupId}.", id);
                    ModelState.AddModelError("", "We were unable to upload the new group logo.");
                    return View(model);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group {GroupId}.", id);
                ModelState.AddModelError("", "We were unable to update the group.");
                return View(model);
            }

            TempData["SuccessMessage"] = "The group was successfully edited.";
            return RedirectToAction("Details", new { id = group.Id });
        }

        // GET: /Group/Delete/{id} - Display confirmation form to delete a group
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var group = await _context.Groups
                .Include(g => g.Creator)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Check if the current user is the creator of the group
            if (group.CreatorId != user.Id)
            {
                return Forbid();
            }

            var model = new DeleteGroupViewModel()
            {
                Id = group.Id,
                GroupName = group.GroupName,
                Creator = group.Creator.UserName
            };

            return View(model);
        }

        // POST: /Group/DeleteConfirmed - Handle group deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DeleteGroupViewModel model)
        {
            var groupToDelete = await _context.Groups
                .Include(g => g.Creator)
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == model.Id);

            if (groupToDelete == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Check if the current user is the creator of the group
            if (groupToDelete.CreatorId != user.Id)
            {
                return Forbid();
            }

            // Remove all members from the group
            groupToDelete.Members.Clear();

            // Delete the group
            _context.Groups.Remove(groupToDelete);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group {GroupId}.", model.Id);
                TempData["ErrorMessage"] = "We were unable to delete the group.";
                return View(model);
            }

            TempData["SuccessMessage"] = "The group was successfully deleted.";
            return RedirectToAction(nameof(All));
        }

        // POST: /Group/RemoveMember - Remove a member from the group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Check if the current user is the creator of the group
            if (group.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            var userToRemove = await _userManager.FindByIdAsync(userId.ToString());
            if (userToRemove == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Prevent removing the group creator
            if (userToRemove.Id == group.CreatorId)
            {
                TempData["ErrorMessage"] = "You cannot remove the creator of the group.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Check if the user is a member of the group
            if (!group.Members.Any(m => m.Id == userToRemove.Id))
            {
                TempData["ErrorMessage"] = "The user is not a member of the group.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Remove the user from the group
            group.Members.Remove(userToRemove);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member {UserId} from group {GroupId}.", userId, groupId);
                TempData["ErrorMessage"] = "We were unable to remove the user from the group.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Check if the user is still a member of any group or creator of any group
            var isStillMember = await _context.Groups.AnyAsync(g => g.Members.Any(m => m.Id == userToRemove.Id));
            var isCreator = await _context.Groups.AnyAsync(g => g.CreatorId == userToRemove.Id);

            if (!isStillMember && !isCreator)
            {
                if (await _userManager.IsInRoleAsync(userToRemove, "GroupMember"))
                {
                    var roleResult = await _userManager.RemoveFromRoleAsync(userToRemove, "GroupMember");
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError("Failed to remove user {UserId} from role 'GroupMember'.", userToRemove.Id);
                        TempData["ErrorMessage"] = "We were unable to remove the user from the role. 'GroupMember'.";
                        return RedirectToAction("Details", new { id = groupId });
                    }
                }
            }

            TempData["SuccessMessage"] = "The user was successfully removed from the group.";
            return RedirectToAction("Details", new { id = groupId });
        }

        // Асинхронен метод за качване на лого
        private async Task<string> UploadLogoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return DefaultLogo();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var permittedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Unsupported image format.");
            }

            if (!permittedMimeTypes.Contains(file.ContentType))
            {
                throw new InvalidOperationException("Unsupported MIME type.");
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

        // Връща път до стандартното лого
        private string DefaultLogo()
        {
            return "/Pictures/DefaultUser.png";
        }

        // Връща път до стандартната профилна снимка
        private string DefaultProfilePicture()
        {
            return "/Pictures/DefaultUser.png";
        }
    }
}
