using JamSpotApp.Controllers;
using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.Message;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using System.Security.Claims;

namespace JamSpotApp.Tests.Controllers
{
    [TestFixture]
    public class MessagesControllerTests
    {
        private DbContextOptions<JamSpotDbContext> _options;

        [SetUp]
        public void Setup()
        {
            _options = new DbContextOptionsBuilder<JamSpotDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // Ignore transaction warnings
                .Options;

            using (var context = new JamSpotDbContext(_options))
            {
                context.Database.EnsureCreated();
            }
        }

        private ClaimsPrincipal GetAdminClaimsPrincipal()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }

        private ClaimsPrincipal GetUserClaimsPrincipal()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "User"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }

        [Test]
        public void Index_AdminRole_ReturnsFilteredMessages()
        {
            using (var context = new JamSpotDbContext(_options))
            {
                // Arrange
                var userId1 = Guid.NewGuid();
                var userId2 = Guid.NewGuid();

                var messages = new List<Message>
        {
            new Message
            {
                Id = Guid.NewGuid(),
                Title = "Pinned Message",
                Content = "Content 1",
                CreatedAt = DateTime.Now.AddDays(-1),
                Pinned = true,
                UserId = userId1,
                Username = new User { Id = userId1, UserName = "User1" }
            },
            new Message
            {
                Id = Guid.NewGuid(),
                Title = "Unpinned Message",
                Content = "Content 2",
                CreatedAt = DateTime.Now,
                Pinned = false,
                UserId = userId2,
                Username = new User { Id = userId2, UserName = "User2" }
            }
        };
                context.Messages.AddRange(messages);
                context.SaveChanges();

                var controller = new MessagesController(context)
                {
                    ControllerContext = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext
                        {
                            User = GetAdminClaimsPrincipal()
                        }
                    }
                };

                // Act
                var result = controller.Index("pinned") as ViewResult;

                // Assert
                Assert.IsNotNull(result);
                var model = result.Model as List<MessageViewModel>;
                Assert.IsNotNull(model);
                Assert.AreEqual(1, model.Count);
                Assert.AreEqual("Pinned Message", model.First().Title);
            }
        }

        [Test]
        public void Create_Post_ValidModel_CreatesMessage()
        {
            using (var context = new JamSpotDbContext(_options))
            {
                // Arrange
                var userId = Guid.NewGuid();
                var user = new User
                {
                    Id = userId,
                    UserName = "TestUser"
                };
                context.Users.Add(user);
                context.SaveChanges();

                Console.WriteLine("User Count After Save: " + context.Users.Count());
                var addedUser = context.Users.FirstOrDefault();
                Console.WriteLine($"Added User ID: {addedUser?.Id}, UserName: {addedUser?.UserName}");

                var controller = new MessagesController(context)
                {
                    ControllerContext = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext
                        {
                            User = new ClaimsPrincipal(
                                new ClaimsIdentity(new[]
                                {
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // Задаваме NameIdentifier
                            new Claim(ClaimTypes.Name, "TestUser") // По желание: потребителско име
                                }, "TestAuthentication"))
                        }
                    }
                };

                // Set up TempData
                var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
                controller.TempData = tempData;

                var model = new CreateMessageViewModel
                {
                    Title = "New Message",
                    Content = "This is a test message"
                };

                // Act
                Console.WriteLine("Starting Create Action...");
                var result = controller.Create(model) as RedirectResult;
                Console.WriteLine("Create Action Completed.");

                // Assert
                Assert.IsNotNull(result, "RedirectResult should not be null");
                Assert.AreEqual("/", result.Url, "Redirection URL should match");

                var createdMessage = context.Messages.FirstOrDefault(m => m.Title == "New Message");
                Assert.IsNotNull(createdMessage, "Message should have been created");
                Assert.AreEqual("This is a test message", createdMessage.Content);
                Assert.AreEqual(userId, createdMessage.UserId);
                Assert.AreEqual(user.UserName, createdMessage.Username.UserName);
            }
        }


        [Test]
        public void TogglePin_AdminRole_TogglesMessagePinState()
        {
            using (var context = new JamSpotDbContext(_options))
            {
                // Arrange
                var userId = Guid.NewGuid();

                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    Title = "Message to Toggle",
                    Content = "Content",
                    CreatedAt = DateTime.Now,
                    Pinned = false,
                    UserId = userId,
                    Username = new User { Id = userId, UserName = "TestUser" }
                };
                context.Messages.Add(message);
                context.SaveChanges();

                var controller = new MessagesController(context)
                {
                    ControllerContext = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext
                        {
                            User = GetAdminClaimsPrincipal()
                        }
                    }
                };

                // Act
                var result = controller.TogglePin(message.Id) as RedirectToActionResult;

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("Index", result.ActionName);

                var updatedMessage = context.Messages.Find(message.Id);
                Assert.IsNotNull(updatedMessage);
                Assert.IsTrue(updatedMessage.Pinned);
            }
        }

    }
}
