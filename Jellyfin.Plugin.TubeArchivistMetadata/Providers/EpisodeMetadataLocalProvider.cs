using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Providers
{
    /// <summary>
    /// Local metadata provider which interacts with TubeArchivist library.
    /// </summary>
    public class EpisodeMetadataLocalProvider : ILocalMetadataProvider<Episode>
    {
        private readonly ILogger _logger;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeMetadataLocalProvider"/> class.
        /// </summary>
        /// <param name="mediaEncoder">Media encoder that has a path to ffprobe.</param>
        /// <param name="libraryManager">Library manager.</param>
        /// <exception cref="DataException">If plugin not initialized, throw.</exception>
        public EpisodeMetadataLocalProvider(IMediaEncoder mediaEncoder, ILibraryManager libraryManager)
        {
            if (Plugin.Instance == null)
            {
                throw new DataException("Uninitialized plugin!");
            }
            else
            {
                _logger = Plugin.Instance.Logger;
                _mediaEncoder = mediaEncoder;
                _libraryManager = libraryManager;
            }
        }

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public string Name => "TubeArchivistLocal";

        /// <summary>
        /// Get metadata from video embedded metadata.
        /// </summary>
        /// <param name="info">File info containing a path.</param>
        /// <param name="directoryService">Directory service, not used.</param>
        /// <param name="cancellationToken">Cancellation token passed to ffprobe process.</param>
        /// <returns>Episode metadata obtained from file.</returns>
        public async Task<MetadataResult<Episode>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(info);
            var path = info.Path;

            var enabledLibraries = Plugin.Instance?.Configuration?.GetLocalMetadataEnabledLibrariesSet() ?? new HashSet<string>();

            if (enabledLibraries.Count == 0)
            {
                return new MetadataResult<Episode>();
            }

            var libraries = _libraryManager.GetVirtualFolders();
            var library = libraries.FirstOrDefault(vf =>
                vf.Locations.Any(loc => path.StartsWith(loc, StringComparison.OrdinalIgnoreCase)));

            if (library == null)
            {
                _logger.LogWarning("Video file not found in any library: {Path}", path);
                return new MetadataResult<Episode>();
            }

            if (!enabledLibraries.Contains(library.Name))
            {
                return new MetadataResult<Episode>();
            }

            if (!File.Exists(path))
            {
                _logger.LogWarning("Video file not found: {Path}", path);
                return new MetadataResult<Episode>();
            }

            _logger.LogDebug("Getting metadata for file: {Path}", path);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo { FileName = _mediaEncoder.ProbePath, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };

            process.StartInfo.ArgumentList.Add("-v");
            process.StartInfo.ArgumentList.Add("quiet");
            process.StartInfo.ArgumentList.Add("-show_entries");
            process.StartInfo.ArgumentList.Add("format_tags");
            process.StartInfo.ArgumentList.Add("-of");
            process.StartInfo.ArgumentList.Add("json");
            process.StartInfo.ArgumentList.Add(path);

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                _logger.LogError("ffprobe failed with exit code {ExitCode} for file: {Path}", process.ExitCode, path);
                return new MetadataResult<Episode>();
            }

            var json = JsonConvert.DeserializeObject<dynamic>(output);
            string? taMetadata = json?.format?.tags?.ta;
            if (taMetadata == null)
            {
                return new MetadataResult<Episode>();
            }

            var videoObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(taMetadata);
            var video = videoObj?["video"]?.ToObject<VideoLocal>();
            if (video == null)
            {
                _logger.LogWarning("Failed to parse video metadata from file: {Path}", path);
                return new MetadataResult<Episode>();
            }

            var result = new MetadataResult<Episode> { HasMetadata = true, Item = video.ToEpisode() };
            result.Item.Path = info.Path;

            return result;
        }
    }
}
