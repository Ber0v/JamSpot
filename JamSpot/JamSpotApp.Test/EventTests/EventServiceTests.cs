using JamSpotApp.Controllers;
using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.Event;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace JamSpotApp.Tests.EventTests
{
    [TestFixture] // Added for NUnit
    public class EventServiceTests
    {
        private Mock<UserManager<User>> _userManagerMock;
        private JamSpotDbContext _context;

        [SetUp]
        public void SetUp()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<JamSpotDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new JamSpotDbContext(options);

            // Set up UserManager mock
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task AddEvent_ShouldAddEventToDatabase()
        {
            // Arrange
            var user = new User { UserName = "TestUser", ProfilePicture = "" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new EventController(_context, _userManagerMock.Object);

            // Mock the HttpContext.User
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = userPrincipal }
            };

            var model = new CreateEventViewModel
            {
                EventName = "Test Event",
                EventDescription = "Test Description",
                Location = "Test Location",
                Date = DateTime.Today.ToString("dd-MM-yy")
            };

            // Act
            var result = await controller.CreateEvent(model);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("All", redirectResult.ActionName);

            var events = await _context.Events.Include(e => e.Organizer).ToListAsync();
            Assert.AreEqual(1, events.Count);

            var addedEvent = events[0];
            Assert.AreEqual("Test Event", addedEvent.EventName);
            Assert.AreEqual("Test Description", addedEvent.EventDescription);
            Assert.AreEqual("Test Location", addedEvent.Location);
            Assert.AreEqual(DateTime.Today, addedEvent.Date);
            Assert.AreEqual("TestUser", addedEvent.Organizer.UserName);
        }

        [Test]
        public async Task All_ReturnsViewWithUpcomingEvents()
        {
            // Arrange
            var user = new User { UserName = "TestUser", ProfilePicture = "" };
            var futureEvent = new Event
            {
                EventName = "Future Event",
                EventDescription = "Future Event Description",
                Location = "Test Location",
                Date = DateTime.Today.AddDays(1),
                Organizer = user
            };
            _context.Events.Add(futureEvent);
            await _context.SaveChangesAsync();

            var controller = new EventController(_context, _userManagerMock.Object);

            // Act
            var result = await controller.All();

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            var model = viewResult.Model as IEnumerable<EventViewModel>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count());
        }

        [Test]
        public void CreateEvent_Get_ReturnsView()
        {
            // Arrange
            var controller = new EventController(_context, _userManagerMock.Object);

            // Act
            var result = controller.CreateEvent();

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            var model = viewResult.Model as CreateEventViewModel;
            Assert.IsNotNull(model);
        }

        [Test]
        public async Task CreateEvent_Post_WithInvalidDate_ReturnsViewWithModelError()
        {
            // Arrange
            var user = new User { UserName = "TestUser", ProfilePicture = "" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new EventController(_context, _userManagerMock.Object);

            var model = new CreateEventViewModel
            {
                EventName = "Test Event",
                EventDescription = "Test Description",
                Location = "Test Location",
                Date = "invalid-date"
            };

            // Act
            var result = await controller.CreateEvent(model);

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            Assert.IsFalse(controller.ModelState.IsValid);
            Assert.IsTrue(controller.ModelState.ContainsKey(nameof(CreateEventViewModel.Date)));
        }

        [Test]
        public async Task Delete_Get_ReturnsViewWithEvent()
        {
            // Arrange
            var user = new User { UserName = "TestUser", ProfilePicture = "" };
            var testEvent = new Event
            {
                Id = Guid.NewGuid(),
                EventName = "Test Event",
                EventDescription = "Test Event Description",
                Location = "Test Location",
                Organizer = user
            };
            _context.Events.Add(testEvent);
            await _context.SaveChangesAsync();

            var controller = new EventController(_context, _userManagerMock.Object);

            // Act
            var result = await controller.Delete(testEvent.Id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            var model = viewResult.Model as DeleteEventViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(testEvent.Id, model.Id);
            Assert.AreEqual(testEvent.EventName, model.EventName);
            Assert.AreEqual(testEvent.Organizer.UserName, model.Organizer);
        }

        [Test]
        public async Task Delete_Get_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var controller = new EventController(_context, _userManagerMock.Object);

            // Act
            var result = await controller.Delete(Guid.NewGuid());

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task DeleteConfirmed_RemovesEventAndRedirects()
        {
            // Arrange
            var user = new User { UserName = "TestUser", ProfilePicture = "" };
            var testEvent = new Event
            {
                Id = Guid.NewGuid(),
                EventName = "Test Event",
                EventDescription = "Test Event Description",
                Location = "Test Location",
                Organizer = user
            };
            _context.Events.Add(testEvent);
            await _context.SaveChangesAsync();

            var controller = new EventController(_context, _userManagerMock.Object);

            var model = new DeleteEventViewModel
            {
                Id = testEvent.Id,
                EventName = testEvent.EventName,
                Organizer = testEvent.Organizer.UserName
            };

            // Act
            var result = await controller.DeleteConfirmed(model);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("All", redirectResult.ActionName);

            var eventInDb = await _context.Events.FindAsync(testEvent.Id);
            Assert.IsNull(eventInDb);
        }

        [Test]
        public async Task DeleteConfirmed_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var controller = new EventController(_context, _userManagerMock.Object);

            var model = new DeleteEventViewModel
            {
                Id = Guid.NewGuid(),
                EventName = "Non-existent Event",
                Organizer = "Unknown"
            };

            // Act
            var result = await controller.DeleteConfirmed(model);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task CreateEvent_Post_WithInvalidModel_ReturnsViewWithErrors()
        {
            // Arrange
            var controller = new EventController(_context, _userManagerMock.Object);
            controller.ModelState.AddModelError("EventName", "Required");

            var model = new CreateEventViewModel
            {
                EventDescription = "Test Description",
                Location = "Test Location",
                Date = DateTime.Today.ToString("dd-MM-yy")
            };

            // Act
            var result = await controller.CreateEvent(model);

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            Assert.IsFalse(controller.ModelState.IsValid);
            Assert.IsTrue(controller.ModelState.ContainsKey("EventName"));
        }

        [Test]
        public async Task All_ReturnsEmptyViewWhenNoEvents()
        {
            // Arrange
            var controller = new EventController(_context, _userManagerMock.Object);

            // Act
            var result = await controller.All();

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            var model = viewResult.Model as IEnumerable<EventViewModel>;
            Assert.IsNotNull(model);
            Assert.IsEmpty(model);
        }

        [Test]
        public async Task All_DoesNotReturnPastEvents()
        {
            // Arrange
            var user = new User { UserName = "TestUser", ProfilePicture = "" };
            var pastEvent = new Event
            {
                EventName = "Past Event",
                EventDescription = "Past Event Description",
                Location = "Test Location",
                Date = DateTime.Today.AddDays(-1),
                Organizer = user
            };
            _context.Events.Add(pastEvent);
            await _context.SaveChangesAsync();

            var controller = new EventController(_context, _userManagerMock.Object);

            // Act
            var result = await controller.All();

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);
            var model = viewResult.Model as IEnumerable<EventViewModel>;
            Assert.IsNotNull(model);
            Assert.IsEmpty(model);
        }
    }
}
