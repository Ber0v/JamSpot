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
        public async Task DeleteOldEventsService_ShouldDeleteOnlyPastEvents()
        {
            // Arrange: Populate database with events
            using (var context = new JamSpotDbContext(_dbContextOptions))
            {
                context.Events.AddRange(
                    new Event
                    {
                        EventName = "Old Event",
                        EventDescription = "This is an old event.",
                        Location = "Location X",
                        Date = DateTime.Today.AddDays(-40),
                        Organizer = new User { UserName = "Organizer1" }
                    },
                    new Event
                    {
                        EventName = "Upcoming Event",
                        EventDescription = "This is a future event.",
                        Location = "Location Z",
                        Date = DateTime.Today.AddDays(10),
                        Organizer = new User { UserName = "Organizer2" }
                    });

                await context.SaveChangesAsync();
            }

            // Use a short delay for testing
            var deleteOldEventsService = new DeleteOldEventsService(_serviceProviderMock.Object, TimeSpan.FromMilliseconds(100));

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(200); // Allow enough time for one execution

            // Act: Start the service
            await deleteOldEventsService.StartAsync(cancellationTokenSource.Token);

            // Assert: Validate the remaining events in the database
            using (var context = new JamSpotDbContext(_dbContextOptions))
            {
                var remainingEvents = await context.Events.ToListAsync();
                Assert.AreEqual(1, remainingEvents.Count, "Only future events should remain in the database.");
                Assert.AreEqual("Upcoming Event", remainingEvents[0].EventName, "The future event should not be deleted.");
            }
        }

        [Test]
        public async Task DeleteOldEventsService_ShouldNotFailWithEmptyDatabase()
        {
            // Arrange: Ensure database is empty
            using (var context = new JamSpotDbContext(_dbContextOptions))
            {
                Assert.AreEqual(0, await context.Events.CountAsync(), "Database should be empty at the start.");
            }

            var deleteOldEventsService = new DeleteOldEventsService(_serviceProviderMock.Object);

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(100); // Small delay to allow single loop execution

            // Act: Start the service
            await deleteOldEventsService.StartAsync(cancellationTokenSource.Token);

            // Assert: No exceptions or changes in an empty database
            using (var context = new JamSpotDbContext(_dbContextOptions))
            {
                var remainingEvents = await context.Events.ToListAsync();
                Assert.AreEqual(0, remainingEvents.Count, "Database should remain empty.");
            }
        }
    }
}
