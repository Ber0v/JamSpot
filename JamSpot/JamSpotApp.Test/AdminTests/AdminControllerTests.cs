using JamSpotApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace JamSpotApp.Tests.Controllers
{
    [TestFixture]
    public class AdminControllerTests
    {
        [Test]
        public void ToggleAdminMode_EnablesAdminMode_SetsCookie()
        {
            // Arrange
            var controller = new AdminController();
            var mockResponse = new Mock<HttpResponse>();
            var cookies = new Mock<IResponseCookies>();
            mockResponse.Setup(r => r.Cookies).Returns(cookies.Object);

            var context = new Mock<HttpContext>();
            context.Setup(c => c.Response).Returns(mockResponse.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = context.Object
            };

            // Act
            var result = controller.ToggleAdminMode(true);

            // Assert
            cookies.Verify(c => c.Append("IsAdminMode", "true",
                It.Is<CookieOptions>(o => o.HttpOnly == true && o.Expires > DateTimeOffset.Now)),
                Times.Once, "Expected the 'IsAdminMode' cookie to be set.");

            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult.");
            Assert.AreEqual("/", redirectResult.Url, "Expected a redirect to the root URL.");
        }

        [Test]
        public void ToggleAdminMode_DisablesAdminMode_DeletesCookie()
        {
            // Arrange
            var controller = new AdminController();
            var mockResponse = new Mock<HttpResponse>();
            var cookies = new Mock<IResponseCookies>();
            mockResponse.Setup(r => r.Cookies).Returns(cookies.Object);

            var context = new Mock<HttpContext>();
            context.Setup(c => c.Response).Returns(mockResponse.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = context.Object
            };

            // Act
            var result = controller.ToggleAdminMode(false);

            // Assert
            cookies.Verify(c => c.Delete("IsAdminMode"), Times.Once, "Expected the 'IsAdminMode' cookie to be deleted.");

            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult.");
            Assert.AreEqual("/", redirectResult.Url, "Expected a redirect to the root URL.");
        }
    }
}
