using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Resolvers
{
    /// <summary>
    /// Resolves each video file of the TubeArchivist library as its own episode.
    /// </summary>
    /// <remarks>
    /// Jellyfin 12 parses a season and an episode number from the path of each episode file.
    /// It groups the files of one folder with the same numbers as versions.
    /// A YouTube ID can hold such numbers by chance.
    /// </remarks>
    public class TubeArchivistEpisodeResolver : IItemResolver, IMultiItemResolver
    {
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TubeArchivistEpisodeResolver"/> class.
        /// </summary>
        /// <param name="libraryManager">Library manager.</param>
        public TubeArchivistEpisodeResolver(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        /// <inheritdoc />
        public ResolverPriority Priority => ResolverPriority.Plugin;

        /// <inheritdoc />
        public BaseItem? ResolvePath(ItemResolveArgs args) => null;

        /// <summary>
        /// Resolves the files of a folder of the TubeArchivist library one by one.
        /// </summary>
        /// <param name="parent">The folder that holds the files.</param>
        /// <param name="files">The files and folders in the parent folder.</param>
        /// <param name="collectionType">The collection type of the library.</param>
        /// <param name="directoryService">Directory service.</param>
        /// <returns>The resolved items, or null if the folder is not in the TubeArchivist library.</returns>
        public MultiItemResolverResult ResolveMultiple(
            Folder parent,
            List<FileSystemMetadata> files,
            CollectionType? collectionType,
            IDirectoryService directoryService)
        {
            var collectionTitle = Plugin.Instance?.Configuration.CollectionTitle;
            if (collectionType != CollectionType.tvshows
                || !_libraryManager.GetCollectionFolders(parent).Any(library => string.Equals(library.Name, collectionTitle, StringComparison.OrdinalIgnoreCase)))
            {
                // Null gives the folder to the built-in resolvers.
                return null!;
            }

            var result = new MultiItemResolverResult();
            foreach (var file in files)
            {
                if (file.IsDirectory)
                {
                    result.ExtraFiles.Add(file);
                    continue;
                }

                // The single file resolvers do not group files as versions and do not stack them.
                var item = _libraryManager.ResolvePath(file, parent, directoryService, collectionType);
                if (item is not null)
                {
                    result.Items.Add(item);
                }
            }

            // The built-in multiple file resolver sets the same value.
            var isInMixedFolder = result.Items.Count > 1 || parent.IsTopParent;
            foreach (var item in result.Items)
            {
                item.IsInMixedFolder = isInMixedFolder;
            }

            return result;
        }
    }
}
