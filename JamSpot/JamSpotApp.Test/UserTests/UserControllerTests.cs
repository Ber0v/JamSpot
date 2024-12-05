using JamSpotApp.Controllers;
using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace JamSpotApp.Tests.Controllers
{
    [TestFixture]
    public class UserControllerTests
    {
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<SignInManager<User>> _mockSignInManager;
        private Mock<ILogger<UserController>> _mockLogger;
        private DbContextOptions<JamSpotDbContext> _options;

        [SetUp]
        public void Setup()
        {
            _options = new DbContextOptionsBuilder<JamSpotDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // Игнорира предупреждения за транзакции
                .Options;

            using (var context = new JamSpotDbContext(_options))
            {
                context.Database.EnsureCreated();
            }

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

            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                null,
                null,
                null,
                null);

            _mockLogger = new Mock<ILogger<UserController>>();
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
        public async Task All_ReturnsCurrentUserProfile()
        {
            using (var context = new JamSpotDbContext(_options))
            {
                // Arrange
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = "TestUser",
                    UserBio = "This is a test bio",
                    InstagramUrl = "https://instagram.com/testuser",
                    FacebookUrl = "https://facebook.com/testuser"
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(user);

                var controller = new UserController(context, _mockUserManager.Object, _mockSignInManager.Object, _mockLogger.Object);
                controller.ControllerContext.HttpContext = new DefaultHttpContext
                {
                    User = GetClaimsPrincipal(user)
                };

                // Act
                var result = await controller.All(null);

                // Assert
                var viewResult = result as ViewResult;
                Assert.IsNotNull(viewResult);

                var model = viewResult.Model as UserViewModel;
                Assert.IsNotNull(model);
                Assert.AreEqual(user.UserName, model.UserName);
                Assert.AreEqual(user.UserBio, model.UserBio);
            }
        }

        [Test]
        public async Task Edit_Get_UserExists_ReturnsView()
        {
            using (var context = new JamSpotDbContext(_options))
            {
                // Arrange
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = "TestUser"
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                    .Returns(user.Id.ToString());

                var controller = new UserController(context, _mockUserManager.Object, _mockSignInManager.Object, _mockLogger.Object);
                controller.ControllerContext.HttpContext = new DefaultHttpContext
                {
                    User = GetClaimsPrincipal(user)
                };

                // Act
                var result = await controller.Edit(user.Id);

                // Assert
                var viewResult = result as ViewResult;
                Assert.IsNotNull(viewResult);

                var model = viewResult.Model as EditUserViewModel;
                Assert.IsNotNull(model);
                Assert.AreEqual(user.UserName, model.UserName);
            }
        }

        [Test]
        public async Task DeleteConfirmed_DeletesUser_RedirectsToDelete()
        {
            using (var context = new JamSpotDbContext(_options))
            {
                // Arrange
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = "TestUser"
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                    .Returns(user.Id.ToString());

                var controller = new UserController(context, _mockUserManager.Object, _mockSignInManager.Object, _mockLogger.Object);
                controller.ControllerContext.HttpContext = new DefaultHttpContext
                {
                    User = GetClaimsPrincipal(user)
                };

                var model = new DeleteUserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName
                };

                // Assert: Уверете се, че потребителят съществува в базата данни преди изтриване
                var existingUser = await context.Users.FindAsync(user.Id);
                Assert.IsNotNull(existingUser, "User should exist before deletion");

                // Act
                var result = await controller.DeleteConfirmed(model);

                // Assert: Проверете резултата от действието
                var redirectResult = result as RedirectToActionResult;
                Assert.IsNotNull(redirectResult, "Redirect result should not be null");
                Assert.AreEqual("Delete", redirectResult.ActionName);

                // Assert: Уверете се, че потребителят е изтрит от базата данни
                var deletedUser = await context.Users.FindAsync(user.Id);
                Assert.IsNull(deletedUser, "User should be deleted from the database");
            }
        }
    }
}
