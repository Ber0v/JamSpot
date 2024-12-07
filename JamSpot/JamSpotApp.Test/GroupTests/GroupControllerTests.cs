using JamSpotApp.Controllers;
using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.Group;
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
    public class GroupControllerTests
    {
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<ILogger<GroupController>> _mockLogger;
        private DbContextOptions<JamSpotDbContext> _options;

        [SetUp]
        public void Setup()
        {
            _options = new DbContextOptionsBuilder<JamSpotDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var userStoreMock = new Mock<IUserStore<User>>();

            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<User>>(),
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            _mockLogger = new Mock<ILogger<GroupController>>();
        }

        private ClaimsPrincipal GetClaimsPrincipal(User user)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName)
        };

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }

        private string DefaultLogo()
        {
            return "/default/logo.png"; // Replace with actual logic if needed
        }

        [Test]
        public async Task All_ReturnsGroups_WithPagination()
        {
            using (var context = new JamSpotDbContext(_options))
            {
                // Arrange
                var user = new User { Id = Guid.NewGuid(), UserName = "CreatorUser" };
                context.Users.Add(user);

                // Initialize Group with all required properties
                context.Groups.AddRange(new List<Group>
        {
            new Group { Id = Guid.NewGuid(), GroupName = "Group 1", Creator = user, CreatorId = user.Id, Logo = DefaultLogo() },
            new Group { Id = Guid.NewGuid(), GroupName = "Group 2", Creator = user, CreatorId = user.Id, Logo = DefaultLogo() }
        });
                await context.SaveChangesAsync();

                var controller = new GroupController(context, _mockUserManager.Object, _mockLogger.Object);

                // Act
                var result = await controller.All(null, 1);

                // Assert
                var viewResult = result as ViewResult;
                Assert.IsNotNull(viewResult);

                var model = viewResult.Model as GroupListViewModel;
                Assert.IsNotNull(model, "The model returned by the view should not be null.");
                Assert.IsNotNull(model.Groups, "The Groups list should not be null.");
                Assert.AreEqual(2, model.Groups.Count(), "The Groups list should contain 2 items.");
            }
        }
    }
}
