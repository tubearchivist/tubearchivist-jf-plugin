using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Utilities
{
    /// <summary>
    /// Class containing common utils.
    /// </summary>
    public static class Utils
    {
        private const string TAPlaylistIdRegex = @"^(.*)\((.*)\)$";
        private const string YTTAPlaylistNameFormatRegex = @"^(.*)\s\-\s(.*)\s\((.*)\)$";
        private const string TAPlaylistNameFormatRegex = @"^(.*)\s\((.*)\)$";

        /// <summary>
        /// Sanitizes the given URL.
        /// </summary>
        /// <param name="inputUrl">An URL string.</param>
        /// <returns>The URL string without spaces, doubled slashes and with a trailing slash.</returns>
        public static string SanitizeUrl(string inputUrl)
        {
            if (string.IsNullOrWhiteSpace(inputUrl))
            {
                return string.Empty;
            }

            // Extract the schema part (http:// or https://)
            Match schemaMatch = Regex.Match(inputUrl, @"^(?<schema>https?://)", RegexOptions.IgnoreCase);

            // If no schema found, treat whole string as the rest
            int schemaLength = schemaMatch.Success ? schemaMatch.Length : 0;

            // Separate the main part from query (?) and fragment (#)
            string rest = inputUrl.Substring(schemaLength);
            string pathPart = rest;
            string queryAndFragment = string.Empty;

            int qIndex = rest.IndexOf('?', StringComparison.Ordinal);
            int fIndex = rest.IndexOf('#', StringComparison.Ordinal);

            int splitIndex = -1;
            if (qIndex >= 0 && fIndex >= 0)
            {
                splitIndex = Math.Min(qIndex, fIndex);
            }
            else if (qIndex >= 0)
            {
                splitIndex = qIndex;
            }
            else if (fIndex >= 0)
            {
                splitIndex = fIndex;
            }

            if (splitIndex >= 0)
            {
                pathPart = rest.Substring(0, splitIndex);
                queryAndFragment = rest.Substring(splitIndex);
            }

            // Remove double slashes and spaces from the path part
            string cleanedPath = Regex.Replace(pathPart, @"[/\s]+", "/");

            // Remove slashes at the start
            cleanedPath = cleanedPath.TrimStart('/');

            // Add a trailing slash only when there are no query or fragment parts
            if (string.IsNullOrEmpty(queryAndFragment))
            {
                cleanedPath = cleanedPath.TrimEnd('/') + "/";
            }

            // Combine the schema and cleaned path and re-append query/fragment
            string cleanedUrl = (schemaMatch.Success ? schemaMatch.Groups["schema"].Value : string.Empty) + cleanedPath + queryAndFragment;

            return cleanedUrl;
        }

        /// <summary>
        /// Formats episodes and series descriptions replacing newlines with br tags.
        /// </summary>
        /// <param name="description">String to format.</param>
        /// <returns>A string with \n replaced by br tags.</returns>
        public static string FormatDescription(string description)
        {
            if (description == null)
            {
                return string.Empty;
            }

            var maxLength = 500;
            if (Plugin.Instance != null)
            {
                maxLength = Plugin.Instance.Configuration.MaxDescriptionLength;
            }

            if (description.Length > maxLength)
            {
                description = description.Substring(0, maxLength);
            }

            description = description.Replace("\n", "<br>", System.StringComparison.CurrentCulture);

            return description;
        }

        /// <summary>
        /// Gets video name from file path on the disk.
        /// </summary>
        /// <param name="path">File path on disk.</param>
        /// <returns>The video name.</returns>
        public static string GetVideoNameFromPath(string path)
        {
            return path.Split(DetectDirectorySeparator(path)).Last().Split(".").First();
        }

        /// <summary>
        /// Gets channel name from directory path on the disk.
        /// </summary>
        /// <param name="path">Directory path on disk.</param>
        /// <returns>The channel name.</returns>
        public static string GetChannelNameFromPath(string path)
        {
            return path.Split(DetectDirectorySeparator(path)).Last();
        }

        private static char DetectDirectorySeparator(string path)
        {
            int backslashCount = path.Count(c => c == '\\');
            int forwardSlashCount = path.Count(c => c == '/');

            if (backslashCount > forwardSlashCount)
            {
                return '\\'; // Windows directory separator
            }
            else
            {
                return '/'; // Unix directory separator
            }
        }

        /// <summary>
        /// Gets the TubeArchivist playlist id from Jellyfin playlist name.
        /// </summary>
        /// <param name="playlistName">The Jellyfin playlist name.</param>
        /// <returns>The TubeArchvist playlist id.</returns>
        public static string? GetTAPlaylistIdFromName(string playlistName)
        {
            var regex = new Regex(TAPlaylistIdRegex);
            return regex.Match(playlistName).Groups[2].ToString();
        }

        /// <summary>
        /// Gets the TubeArchivist playlist name from Jellyfin playlist name.
        /// </summary>
        /// <param name="playlistName">The Jellyfin playlist name.</param>
        /// <returns>The TubeArchivist playlist name.</returns>
        public static string? GetTAPlaylistNameFromName(string playlistName)
        {
            var ytRegex = new Regex(YTTAPlaylistNameFormatRegex);
            var regex = new Regex(TAPlaylistNameFormatRegex);

            var name = ytRegex.Match(playlistName).Groups[1].ToString();
            if (string.IsNullOrEmpty(name))
            {
                name = regex.Match(playlistName).Groups[1].ToString();
            }

            return name;
        }
    }
}
