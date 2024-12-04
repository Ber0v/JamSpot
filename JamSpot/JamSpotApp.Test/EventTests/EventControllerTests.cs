using JamSpotApp.Controllers;
using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.Event;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace JamSpotApp.Tests.Controllers
{
    [TestFixture]
    public class EventControllerTests
    {
        private Mock<UserManager<User>> mockUserManager;
        private DbContextOptions<JamSpotDbContext> options;

        [SetUp]
        public void Setup()
        {
            options = new DbContextOptionsBuilder<JamSpotDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new JamSpotDbContext(options))
            {
                context.Database.EnsureCreated();
            }

            var userStoreMock = new Mock<IUserStore<User>>();

            mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<User>>(),
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                null, // services
                Mock.Of<ILogger<UserManager<User>>>());
        }

        private ClaimsPrincipal GetClaimsPrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }

        [Test]
        public async Task All_ReturnsViewWithEvents()
        {
            // Arrange
            using (var context = new JamSpotDbContext(options))
            {
                var user = new User { Id = Guid.NewGuid(), UserName = "TestUser" };
                context.Users.Add(user);
                context.Events.AddRange(
                    new Event
                    {
                        Id = Guid.NewGuid(),
                        EventName = "Event1",
                        EventDescription = "Description1",
                        Price = 0,
                        Location = "Location1",
                        Date = DateTime.Today.AddDays(1),
                        Hour = new TimeOnly(12, 0),
                        Organizer = user
                    },
                    new Event
                    {
                        Id = Guid.NewGuid(),
                        EventName = "Event2",
                        EventDescription = "Description2",
                        Price = 10,
                        Location = "Location2",
                        Date = DateTime.Today.AddDays(2),
                        Hour = new TimeOnly(15, 0),
                        Organizer = user
                    }
                );
                await context.SaveChangesAsync();

                mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(user);

                var controller = new EventController(context, mockUserManager.Object);
                controller.ControllerContext.HttpContext = new DefaultHttpContext
                {
                    User = GetClaimsPrincipal(user)
                };

                // Act
                var result = await controller.All(null, null);

                // Assert
                var viewResult = result as ViewResult;
                Assert.IsNotNull(viewResult);

                var model = viewResult.Model as EventListViewModel;
                Assert.IsNotNull(model);
                Assert.IsNotNull(model.Events, "Events collection is null");
                Assert.AreEqual(2, model.Events.Count()); // Уверете се, че използвате Count()
            }
        }

        [Test]
        public async Task Edit_Get_EventExists_ReturnsView()
        {
            // Arrange
            using (var context = new JamSpotDbContext(options))
            {
                var user = new User { Id = Guid.NewGuid(), UserName = "TestUser" };
                var ev = new Event
                {
                    Id = Guid.NewGuid(),
                    EventName = "Event1",
                    EventDescription = "Description",
                    Price = 0,
                    Location = "Location1",
                    Date = DateTime.Today.AddDays(1),
                    Hour = new TimeOnly(12, 0), // Използваме TimeOnly
                    Organizer = user
                };
                context.Users.Add(user);
                context.Events.Add(ev);
                await context.SaveChangesAsync();

                mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(user);

                var controller = new EventController(context, mockUserManager.Object);
                controller.ControllerContext.HttpContext = new DefaultHttpContext
                {
                    User = GetClaimsPrincipal(user)
                };

                // Act
                var result = await controller.Edit(ev.Id);

                // Assert
                var viewResult = result as ViewResult;
                Assert.IsNotNull(viewResult);

                var model = viewResult.Model as CreateEventViewModel;
                Assert.IsNotNull(model);
                Assert.AreEqual(ev.EventName, model.EventName);
            }
        }

        [Test]
        public async Task Details_EventExists_ReturnsView()
        {
            // Arrange
            using (var context = new JamSpotDbContext(options))
            {
                var user = new User { Id = Guid.NewGuid(), UserName = "TestUser" };
                var ev = new Event
                {
                    Id = Guid.NewGuid(),
                    EventName = "Event1",
                    EventDescription = "Description",
                    Price = 0,
                    Location = "Location1",
                    Date = DateTime.Today.AddDays(1),
                    Hour = new TimeOnly(12, 0), // Използваме TimeOnly
                    Organizer = user
                };
                context.Users.Add(user);
                context.Events.Add(ev);
                await context.SaveChangesAsync();

                var controller = new EventController(context, mockUserManager.Object);

                // Act
                var result = await controller.Details(ev.Id);

                // Assert
                var viewResult = result as ViewResult;
                Assert.IsNotNull(viewResult);

                var model = viewResult.Model as EventViewModel;
                Assert.IsNotNull(model);
                Assert.AreEqual(ev.EventName, model.EventName);
            }
        }
    }
}
