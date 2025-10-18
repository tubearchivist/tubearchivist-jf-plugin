#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using Jellyfin.Plugin.TubeArchivistMetadata;
using Jellyfin.Plugin.TubeArchivistMetadata.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
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
    public class CollectionMembershipTests : IDisposable
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

        public CollectionMembershipTests()
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

        private Folder CreateFolder(string name, Guid? id = null, Guid? parentId = null)
        {
            var folderId = id ?? Guid.NewGuid();
            var folder = new Folder();

            var idProperty = typeof(BaseItem).GetProperty("Id");
            idProperty!.SetValue(folder, folderId);

            var nameProperty = typeof(BaseItem).GetProperty("Name");
            nameProperty!.SetValue(folder, name);

            if (parentId.HasValue)
            {
                var parentIdProperty = typeof(BaseItem).GetProperty("ParentId");
                parentIdProperty!.SetValue(folder, parentId.Value);
            }

            return folder;
        }

        private Season CreateSeason(Guid? id = null, Guid? parentId = null)
        {
            var seasonId = id ?? Guid.NewGuid();
            var season = new Season();

            var idProperty = typeof(BaseItem).GetProperty("Id");
            idProperty!.SetValue(season, seasonId);

            if (parentId.HasValue)
            {
                var parentIdProperty = typeof(BaseItem).GetProperty("ParentId");
                parentIdProperty!.SetValue(season, parentId.Value);
            }

            return season;
        }

        private Series CreateSeries(Guid? id = null, Guid? parentId = null)
        {
            var seriesId = id ?? Guid.NewGuid();
            var series = new Series();

            var idProperty = typeof(BaseItem).GetProperty("Id");
            idProperty!.SetValue(series, seriesId);

            if (parentId.HasValue)
            {
                var parentIdProperty = typeof(BaseItem).GetProperty("ParentId");
                parentIdProperty!.SetValue(series, parentId.Value);
            }

            return series;
        }

        private Episode CreateEpisode(Guid? id = null, Guid? parentId = null, string? path = null)
        {
            var episodeId = id ?? Guid.NewGuid();
            var episode = new Episode();

            var idProperty = typeof(BaseItem).GetProperty("Id");
            idProperty!.SetValue(episode, episodeId);

            if (parentId.HasValue)
            {
                var parentIdProperty = typeof(BaseItem).GetProperty("ParentId");
                parentIdProperty!.SetValue(episode, parentId.Value);
            }

            if (path != null)
            {
                var pathProperty = typeof(BaseItem).GetProperty("Path");
                pathProperty!.SetValue(episode, path);
            }

            return episode;
        }

        private bool InvokeIsItemInTubeArchivistCollection(Plugin plugin, BaseItem item)
        {
            var method = typeof(Plugin).GetMethod("IsItemInTubeArchivistCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)method!.Invoke(plugin, new object[] { item })!;
        }

        [Fact]
        public void IsItemInTubeArchivistCollection_WithNonEpisode_ShouldReturnFalse()
        {
            // Arrange
            var collectionId = Guid.NewGuid();
            var collection = CreateFolder("TubeArchivist", collectionId);
            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem> { collection });

            var movie = new Movie();
            var idProperty = typeof(BaseItem).GetProperty("Id");
            idProperty!.SetValue(movie, Guid.NewGuid());

            // Act
            var result = InvokeIsItemInTubeArchivistCollection(plugin, movie);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsItemInTubeArchivistCollection_WithEpisodeDirectlyInCollection_ShouldReturnTrue()
        {
            // Arrange
            var collectionId = Guid.NewGuid();
            var collection = CreateFolder("TubeArchivist", collectionId);
            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem> { collection });

            var episode = CreateEpisode(parentId: collectionId);

            // Setup LibraryManager to return collection when queried
            _mockLibraryManager.Setup(x => x.GetItemById(collectionId)).Returns(collection);

            // Act
            var result = InvokeIsItemInTubeArchivistCollection(plugin, episode);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsItemInTubeArchivistCollection_WithEpisodeInSeasonInSeriesInCollection_ShouldReturnTrue()
        {
            // Arrange
            var collectionId = Guid.NewGuid();
            var seriesId = Guid.NewGuid();
            var seasonId = Guid.NewGuid();

            var collection = CreateFolder("TubeArchivist", collectionId);
            var series = CreateSeries(seriesId, collectionId);
            var season = CreateSeason(seasonId, seriesId);
            var episode = CreateEpisode(parentId: seasonId);

            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem> { collection });

            // Setup LibraryManager to return items in hierarchy
            _mockLibraryManager.Setup(x => x.GetItemById(collectionId)).Returns(collection);
            _mockLibraryManager.Setup(x => x.GetItemById(seriesId)).Returns(series);
            _mockLibraryManager.Setup(x => x.GetItemById(seasonId)).Returns(season);

            // Act
            var result = InvokeIsItemInTubeArchivistCollection(plugin, episode);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsItemInTubeArchivistCollection_WithEpisodeNotInCollection_ShouldReturnFalse()
        {
            // Arrange
            var collectionId = Guid.NewGuid();
            var otherCollectionId = Guid.NewGuid();

            var collection = CreateFolder("TubeArchivist", collectionId);
            var otherCollection = CreateFolder("Other", otherCollectionId);

            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem> { collection });

            var episode = CreateEpisode(parentId: otherCollectionId);

            // Setup LibraryManager
            _mockLibraryManager.Setup(x => x.GetItemById(otherCollectionId)).Returns(otherCollection);

            // Act
            var result = InvokeIsItemInTubeArchivistCollection(plugin, episode);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsItemInTubeArchivistCollection_WithEpisodeWithEmptyParentId_ShouldReturnFalse()
        {
            // Arrange
            var collectionId = Guid.NewGuid();
            var collection = CreateFolder("TubeArchivist", collectionId);
            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem> { collection });

            var episode = CreateEpisode(parentId: Guid.Empty);

            // Act
            var result = InvokeIsItemInTubeArchivistCollection(plugin, episode);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsItemInTubeArchivistCollection_WithNullCachedId_ShouldUseFallbackMethod()
        {
            // Arrange - create plugin with no collections (collection ID won't be cached)
            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem>());

            var collectionId = Guid.NewGuid();
            var seriesId = Guid.NewGuid();
            var seasonId = Guid.NewGuid();

            var collection = CreateFolder("TubeArchivist", collectionId);
            var series = CreateSeries(seriesId, collectionId);
            var season = CreateSeason(seasonId, seriesId);
            var episode = CreateEpisode(parentId: seasonId);

            // Setup LibraryManager to return items when fallback method traverses hierarchy
            _mockLibraryManager.Setup(x => x.GetItemById(seasonId)).Returns(season);
            _mockLibraryManager.Setup(x => x.GetItemById(seriesId)).Returns(series);
            _mockLibraryManager.Setup(x => x.GetItemById(collectionId)).Returns(collection);

            // Act
            var result = InvokeIsItemInTubeArchivistCollection(plugin, episode);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsItemInTubeArchivistCollection_WithDeepHierarchy_ShouldStopAt10Levels()
        {
            // Arrange
            var collectionId = Guid.NewGuid();
            var collection = CreateFolder("TubeArchivist", collectionId);
            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem> { collection });

            // Create a very deep hierarchy (more than 10 levels)
            var currentParentId = Guid.NewGuid();
            var deepestFolder = CreateFolder("Level11", currentParentId);

            for (int i = 10; i > 0; i--)
            {
                var folderId = Guid.NewGuid();
                var folder = CreateFolder($"Level{i}", folderId, currentParentId);
                _mockLibraryManager.Setup(x => x.GetItemById(currentParentId)).Returns(folder);
                currentParentId = folderId;
            }

            var episode = CreateEpisode(parentId: currentParentId);

            // Act
            var result = InvokeIsItemInTubeArchivistCollection(plugin, episode);

            // Assert - should return false because it stops at 10 levels
            result.Should().BeFalse();
        }

        [Fact]
        public void IsItemInTubeArchivistCollection_WithNullParentInHierarchy_ShouldReturnFalse()
        {
            // Arrange
            var collectionId = Guid.NewGuid();
            var collection = CreateFolder("TubeArchivist", collectionId);
            using var plugin = CreatePlugin("TubeArchivist", new List<BaseItem> { collection });

            var seasonId = Guid.NewGuid();
            var episode = CreateEpisode(parentId: seasonId);

            // Setup LibraryManager to return null (broken hierarchy)
            _mockLibraryManager.Setup(x => x.GetItemById(seasonId)).Returns((BaseItem?)null);

            // Act
            var result = InvokeIsItemInTubeArchivistCollection(plugin, episode);

            // Assert
            result.Should().BeFalse();
        }

        public void Dispose()
        {
            _currentPlugin?.Dispose();
        }
    }
}