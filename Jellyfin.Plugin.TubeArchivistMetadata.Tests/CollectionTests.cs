#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentAssertions;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.TubeArchivistMetadata;
using Jellyfin.Plugin.TubeArchivistMetadata.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Tests
{
    public class CollectionTests : IDisposable
    {
        private readonly Mock<IApplicationPaths> _mockApplicationPaths;
        private readonly Mock<IXmlSerializer> _mockXmlSerializer;
        private readonly Mock<ILogger<Plugin>> _mockLogger;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly Mock<ITaskManager> _mockTaskManager;
        private readonly Mock<IUserManager> _mockUserManager;
        private readonly Mock<IUserDataManager> _mockUserDataManager;
        private Plugin? _currentPlugin;

        public CollectionTests()
        {
            _mockApplicationPaths = new Mock<IApplicationPaths>();
            _mockXmlSerializer = new Mock<IXmlSerializer>();
            _mockLogger = new Mock<ILogger<Plugin>>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockLibraryManager = new Mock<ILibraryManager>();
            _mockTaskManager = new Mock<ITaskManager>();
            _mockUserManager = new Mock<IUserManager>();
            _mockUserDataManager = new Mock<IUserDataManager>();

            // Setup required paths
            _mockApplicationPaths.Setup(x => x.PluginConfigurationsPath).Returns("/tmp/jellyfin/plugins");
            _mockApplicationPaths.Setup(x => x.PluginsPath).Returns("/tmp/jellyfin/plugins");

            // Setup defaults
            _mockTaskManager.Setup(x => x.ScheduledTasks).Returns(Array.Empty<IScheduledTaskWorker>());
        }

        private Plugin CreatePlugin(string collectionTitle = "TubeArchivist", List<BaseItem>? collections = null)
        {
            // Create configuration without calling constructor
            var config = (PluginConfiguration)FormatterServices.GetUninitializedObject(typeof(PluginConfiguration));
            config.TubeArchivistApiKey = "";
            config.TubeArchivistUrl = "http://localhost:8000";
            config.CollectionTitle = collectionTitle;
            config.TAJFSync = false;
            config.JFTASync = false;

            _mockXmlSerializer
                .Setup(x => x.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns(config!);

            // Setup library manager to return the provided collections
            _mockLibraryManager
                .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(collections ?? new List<BaseItem>());

            _currentPlugin = new Plugin(
                _mockApplicationPaths.Object,
                _mockXmlSerializer.Object,
                _mockLogger.Object,
                _mockSessionManager.Object,
                _mockLibraryManager.Object,
                _mockTaskManager.Object,
                _mockUserManager.Object,
                _mockUserDataManager.Object
            );

            return _currentPlugin;
        }

        private Folder CreateMockCollection(string name, Guid? id = null)
        {
            var collectionId = id ?? Guid.NewGuid();

            // Create a real Folder instance using reflection to set the Id
            var folder = new Folder();

            // Use reflection to set the Id (it's a protected setter)
            var idProperty = typeof(BaseItem).GetProperty("Id");
            idProperty!.SetValue(folder, collectionId);

            // Use reflection to set the Name
            var nameProperty = typeof(BaseItem).GetProperty("Name");
            nameProperty!.SetValue(folder, name);

            return folder;
        }

        private Episode CreateMockEpisode(Guid parentId)
        {
            var episode = new Episode();

            // Use reflection to set properties
            var idProperty = typeof(BaseItem).GetProperty("Id");
            idProperty!.SetValue(episode, Guid.NewGuid());

            var parentIdProperty = typeof(BaseItem).GetProperty("ParentId");
            parentIdProperty!.SetValue(episode, parentId);

            var pathProperty = typeof(BaseItem).GetProperty("Path");
            pathProperty!.SetValue(episode, "/path/to/video-id.mkv");

            return episode;
        }

        [Fact]
        public void Constructor_WhenCollectionExists_ShouldCacheCollectionId()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            var tubeArchivistCollection = CreateMockCollection("TubeArchivist", expectedId);
            var collections = new List<BaseItem> { tubeArchivistCollection };

            // Act
            using var plugin = CreatePlugin("TubeArchivist", collections);

            // Assert - verify the collection was found and logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Found TubeArchivist collection with ID: {expectedId}")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WhenCollectionNotFound_ShouldLogWarning()
        {
            // Arrange - empty collection list
            var collections = new List<BaseItem>();

            // Act
            using var plugin = CreatePlugin("TubeArchivist", collections);

            // Assert - verify warning was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not find TubeArchivist collection with name: TubeArchivist")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WhenCollectionTitleEmpty_ShouldLogWarning()
        {
            // Arrange
            var collections = new List<BaseItem>();

            // Act
            using var plugin = CreatePlugin("", collections);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TubeArchivist collection title not configured")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithMultipleCollections_ShouldFindCorrectOne()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            var collections = new List<BaseItem>
            {
                CreateMockCollection("Movies"),
                CreateMockCollection("TV Shows"),
                CreateMockCollection("TubeArchivist", expectedId),
                CreateMockCollection("Music")
            };

            // Act
            using var plugin = CreatePlugin("TubeArchivist", collections);

            // Assert - verify the correct collection was found
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Found TubeArchivist collection with ID: {expectedId}")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithCaseInsensitiveMatch_ShouldFindCollection()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            var tubeArchivistCollection = CreateMockCollection("tubearchivist", expectedId); // lowercase
            var collections = new List<BaseItem> { tubeArchivistCollection };

            // Act
            using var plugin = CreatePlugin("TubeArchivist", collections); // PascalCase

            // Assert - verify case-insensitive matching works
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Found TubeArchivist collection with ID: {expectedId}")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void RefreshTubeArchivistCollectionId_WhenCalled_ShouldUpdateCache()
        {
            // Arrange - initially no collections
            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem>());

            // Reset the logger to clear construction logs
            _mockLogger.Reset();

            // Now setup library manager to return a collection
            var newCollectionId = Guid.NewGuid();
            var newCollection = CreateMockCollection("TubeArchivist", newCollectionId);
            _mockLibraryManager
                .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { newCollection });

            // Act
            plugin.RefreshTubeArchivistCollectionId();

            // Assert - verify the collection was found after refresh
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Refreshing TubeArchivist collection cache")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Found TubeArchivist collection with ID: {newCollectionId}")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void RefreshTubeArchivistCollectionId_WithNewTitle_ShouldFindNewCollection()
        {
            // Arrange
            var oldCollection = CreateMockCollection("OldCollection", Guid.NewGuid());
            using var plugin = CreatePlugin("OldCollection", new List<BaseItem> { oldCollection });

            _mockLogger.Reset();

            // Setup new collection with different name
            var newCollectionId = Guid.NewGuid();
            var newCollection = CreateMockCollection("NewCollection", newCollectionId);
            _mockLibraryManager
                .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { newCollection });

            // Act
            plugin.RefreshTubeArchivistCollectionId("NewCollection");

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Found TubeArchivist collection with ID: {newCollectionId}")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        public void Dispose()
        {
            _currentPlugin?.Dispose();
        }
    }
}