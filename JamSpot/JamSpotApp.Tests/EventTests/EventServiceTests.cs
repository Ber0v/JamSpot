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
using Xunit;
using Assert = Xunit.Assert;


namespace JamSpotApp.Tests.EventTests
{
    public class EventServiceTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly JamSpotDbContext _context;

        public EventServiceTests()
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

        [Fact]
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
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("All", redirectResult.ActionName);

            var events = await _context.Events.Include(e => e.Organizer).ToListAsync();
            Assert.Single(events);

            var addedEvent = events[0];
            Assert.Equal("Test Event", addedEvent.EventName);
            Assert.Equal("Test Description", addedEvent.EventDescription);
            Assert.Equal("Test Location", addedEvent.Location);
            Assert.Equal(DateTime.Today, addedEvent.Date);
            Assert.Equal("TestUser", addedEvent.Organizer.UserName);
        }

        // Additional test methods

        [Fact]
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
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<EventViewModel>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public void CreateEvent_Get_ReturnsView()
        {
            // Arrange
            var controller = new EventController(_context, _userManagerMock.Object);

            // Act
            var result = controller.CreateEvent();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<CreateEventViewModel>(viewResult.ViewData.Model);
        }

        [Fact]
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
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(CreateEventViewModel.Date)));
        }
    }
}
