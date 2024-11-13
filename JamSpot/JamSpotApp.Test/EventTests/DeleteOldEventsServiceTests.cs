using JamSpotApp.Data;
using JamSpotApp.Data.Models;
using JamSpotApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace JamSpotApp.Tests.EventTests
{
    [TestFixture]
    public class DeleteOldEventsServiceTests
    {
        private DbContextOptions<JamSpotDbContext> _dbContextOptions;
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IServiceScopeFactory> _scopeFactoryMock;
        private Mock<IServiceScope> _scopeMock;

        [SetUp]
        public void SetUp()
        {
            _dbContextOptions = new DbContextOptionsBuilder<JamSpotDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _serviceProviderMock = new Mock<IServiceProvider>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();

            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
            _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);

            _serviceProviderMock.Setup(x => x.GetService(typeof(JamSpotDbContext)))
                                .Returns(new JamSpotDbContext(_dbContextOptions));

            _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                                .Returns(_scopeFactoryMock.Object);
        }

        [Test]
        public async Task DeleteOldEventsService_ShouldDeletePastEvents()
        {
            // Arrange
            using (var context = new JamSpotDbContext(_dbContextOptions))
            {
                var pastEvent = new Event
                {
                    EventName = "Past Event",
                    EventDescription = "This is a past event.",
                    Location = "Location A",
                    Date = DateTime.Today.AddDays(-2),
                    Organizer = new User { UserName = "Organizer1", ProfilePicture = "Profile1.jpg" }
                };

                var futureEvent = new Event
                {
                    EventName = "Future Event",
                    EventDescription = "This is a future event.",
                    Location = "Location B",
                    Date = DateTime.Today.AddDays(2),
                    Organizer = new User { UserName = "Organizer2", ProfilePicture = "Profile2.jpg" }
                };

                context.Events.AddRange(pastEvent, futureEvent);
                await context.SaveChangesAsync();
            }

            var deleteOldEventsService = new DeleteOldEventsService(_serviceProviderMock.Object);

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(2000); // Increase timeout to 2 seconds

            // Act
            await deleteOldEventsService.StartAsync(cancellationTokenSource.Token);

            // Assert
            using (var context = new JamSpotDbContext(_dbContextOptions))
            {
                var events = await context.Events.ToListAsync();
                Assert.AreEqual(1, events.Count);
                Assert.AreEqual("Future Event", events[0].EventName);
            }
        }
    }
}
