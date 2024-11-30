using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.Message;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JamSpotApp.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly JamSpotDbContext _context;

        public MessagesController(JamSpotDbContext context)
        {
            _context = context;
        }

        public List<Message> Messages { get; set; }

        // Преглед на съобщения (за администратори)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Index(string filter = "all", string searchTerm = null)
        {
            ViewData["CurrentFilter"] = filter;
            ViewData["SearchTerm"] = searchTerm;

            IQueryable<Message> query = _context.Messages;

            switch (filter)
            {
                case "pinned":
                    query = query.Where(m => m.Pinned);
                    break;
                case "unpinned":
                    query = query.Where(m => !m.Pinned);
                    break;
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m => m.Username.UserName.Contains(searchTerm));
            }

            var messages = query
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    Username = m.Username.UserName,
                    Title = m.Title,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    IsFromAdmin = m.IsFromAdmin,
                    Pinned = m.Pinned
                })
                .ToList();

            return View(messages);
        }


        // Създаване на съобщение (за потребители)
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateMessageViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateMessageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = _context.Users.Find(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            var message = new Message
            {
                Title = model.Title,
                Content = model.Content,
                CreatedAt = DateTime.Now,
                UserId = Guid.Parse(userId),
                Username = user,
            };

            _context.Messages.Add(message);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Your message was sent successfully!";
            return Redirect("/");
        }

        // Създаване на съобщение от администратора
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateAdminMessage()
        {
            return View(new CreateMessageViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAdminMessage(CreateMessageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminId == null)
            {
                return Unauthorized();
            }

            var admin = _context.Users.Find(Guid.Parse(adminId));
            if (admin == null)
            {
                return NotFound();
            }

            var message = new Message
            {
                Content = model.Content,
                Title = model.Title,
                CreatedAt = DateTime.Now,
                UserId = Guid.Parse(adminId),
                Username = admin,
                IsFromAdmin = true,
                Pinned = true // Всички администраторски съобщения се пинват
            };

            _context.Messages.Add(message);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // Пинване/отпинване на съобщение
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult TogglePin(Guid id, string filter = "all")
        {
            var message = _context.Messages.Find(id);
            if (message == null)
            {
                return NotFound();
            }

            message.Pinned = !message.Pinned;
            _context.SaveChanges();

            return RedirectToAction("Index", new { filter });
        }

        // Изтриване на съобщение
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id, string filter = "all")
        {
            var message = _context.Messages.Find(id);
            if (message == null)
            {
                return NotFound();
            }

            _context.Messages.Remove(message);
            _context.SaveChanges();

            return RedirectToAction("Index", new { filter });
        }

    }
}
