using JamSpotApp.Controllers;
using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.feed;
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
    public class FeedControllerTests
    {
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<ILogger<FeedController>> _mockLogger;
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

            _mockLogger = new Mock<ILogger<FeedController>>();
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

        [Test]
        public async Task All_ReturnsPosts_WithPagination()
        {
            using (var context = new JamSpotDbContext(_options))
            {
                // Arrange
                var userId = Guid.NewGuid();
                var user = new User { Id = userId, UserName = "TestUser" };

                var group = new Group
                {
                    Id = Guid.NewGuid(),
                    GroupName = "TestGroup",
                    CreatorId = userId,
                    Creator = user,
                    Logo = "/default/logo.png" // Setting required 'Logo' property
                };

                var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), Title = "Post 1", Content = "Content 1", CreatedDate = DateTime.Now.AddHours(-1), User = user },
            new Post { Id = Guid.NewGuid(), Title = "Post 2", Content = "Content 2", CreatedDate = DateTime.Now, Group = group }
        };

                context.Users.Add(user);
                context.Groups.Add(group);
                context.Posts.AddRange(posts);
                await context.SaveChangesAsync();

                _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(user);

                var controller = new FeedController(context, _mockUserManager.Object, _mockLogger.Object)
                {
                    ControllerContext = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext
                        {
                            User = GetClaimsPrincipal(user)
                        }
                    }
                };

                // Act
                var result = await controller.All(null, 1);

                // Assert
                var viewResult = result as ViewResult;
                Assert.IsNotNull(viewResult, "Expected a ViewResult.");

                var model = viewResult.Model as FeedPageViewModel;
                Assert.IsNotNull(model, "Expected the model to be a FeedPageViewModel.");

                var postList = model.Posts.ToList(); // Convert to a list for indexing
                Assert.AreEqual(2, postList.Count, "Expected 2 posts to be returned.");
                Assert.AreEqual("Post 2", postList[0].Title, "Expected the latest post to appear first.");
                Assert.AreEqual("Post 1", postList[1].Title, "Expected the older post to appear second.");
            }
        }
    }
}