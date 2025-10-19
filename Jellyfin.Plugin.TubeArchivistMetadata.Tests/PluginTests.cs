#nullable enable

using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.TubeArchivistMetadata;
using Jellyfin.Plugin.TubeArchivistMetadata.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Tests
{
    public class PluginTests
    {
        private readonly Mock<IApplicationPaths> _mockApplicationPaths;
        private readonly Mock<IXmlSerializer> _mockXmlSerializer;
        private readonly Mock<ILogger<Plugin>> _mockLogger;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly Mock<ITaskManager> _mockTaskManager;
        private readonly Mock<IUserManager> _mockUserManager;
        private readonly Mock<IUserDataManager> _mockUserDataManager;

        public PluginTests()
        {
            _mockApplicationPaths = new Mock<IApplicationPaths>();
            _mockXmlSerializer = new Mock<IXmlSerializer>();
            _mockLogger = new Mock<ILogger<Plugin>>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockLibraryManager = new Mock<ILibraryManager>();
            _mockTaskManager = new Mock<ITaskManager>();
            _mockUserManager = new Mock<IUserManager>();
            _mockUserDataManager = new Mock<IUserDataManager>();

            // Setup required paths for BasePlugin
            _mockApplicationPaths.Setup(x => x.PluginConfigurationsPath).Returns("/tmp/jellyfin/plugins");
            _mockApplicationPaths.Setup(x => x.PluginsPath).Returns("/tmp/jellyfin/plugins");

            // Create configuration without calling constructor (bypasses Plugin.Instance check)
            var config = (PluginConfiguration)FormatterServices.GetUninitializedObject(typeof(PluginConfiguration));
            config.TubeArchivistApiKey = "";
            config.TubeArchivistUrl = "http://localhost:8000";
            config.CollectionTitle = "TubeArchivist";
            config.TAJFSync = false;
            config.JFTASync = false;

            // Setup XmlSerializer to return the config
            _mockXmlSerializer
                .Setup(x => x.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns(config!);

            // Setup defaults
            _mockTaskManager.Setup(x => x.ScheduledTasks).Returns(Array.Empty<IScheduledTaskWorker>());
            _mockLibraryManager.Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(Array.Empty<MediaBrowser.Controller.Entities.BaseItem>().ToList());
        }
        private Plugin CreatePlugin()
        {
            return new Plugin(
                _mockApplicationPaths.Object,
                _mockXmlSerializer.Object,
                _mockLogger.Object,
                _mockSessionManager.Object,
                _mockLibraryManager.Object,
                _mockTaskManager.Object,
                _mockUserManager.Object,
                _mockUserDataManager.Object
            );
        }

        [Fact]
        public void Constructor_ShouldInitializeHttpClient()
        {
            // Act
            using var plugin = CreatePlugin();

            // Assert
            plugin.HttpClient.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldSubscribeToEvents()
        {
            // Act
            using var plugin = CreatePlugin();

            // Assert
            _mockSessionManager.VerifyAdd(x => x.PlaybackProgress += It.IsAny<EventHandler<PlaybackProgressEventArgs>>(), Times.Once);
            _mockUserDataManager.VerifyAdd(x => x.UserDataSaved += It.IsAny<EventHandler<UserDataSaveEventArgs>>(), Times.Once);
        }

        [Fact]
        public async Task Dispose_ShouldDisposeHttpClient()
        {
            // Arrange
            var plugin = CreatePlugin();
            var httpClient = plugin.HttpClient;

            // Act
            plugin.Dispose();

            // Assert
            Func<Task> act = async () => await httpClient.GetAsync("http://test.com");
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromPlaybackProgress()
        {
            // Arrange
            var plugin = CreatePlugin();

            // Act
            plugin.Dispose();

            // Assert
            _mockSessionManager.VerifyRemove(x => x.PlaybackProgress -= It.IsAny<EventHandler<PlaybackProgressEventArgs>>(), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromUserDataSaved()
        {
            // Arrange
            var plugin = CreatePlugin();

            // Act
            plugin.Dispose();

            // Assert
            _mockUserDataManager.VerifyRemove(x => x.UserDataSaved -= It.IsAny<EventHandler<UserDataSaveEventArgs>>(), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent()
        {
            // Arrange
            var plugin = CreatePlugin();

            // Act
            plugin.Dispose();
            plugin.Dispose(); // Second dispose should not throw

            // Assert - no exception thrown
        }

        [Fact]
        public void UpdateAuthorizationHeader_WithEmptyApiKey_ShouldLogWarning()
        {
            // Arrange
            using var plugin = CreatePlugin();

            // Reset mock to clear the warning from plugin construction
            _mockLogger.Reset();

            // Act
            plugin.UpdateAuthorizationHeader(string.Empty);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No TubeArchivist API key")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
