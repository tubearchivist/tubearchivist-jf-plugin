using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Utilities
{
    /// <summary>
    /// Class containing common utils.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Sanitize the given URL.
        /// </summary>
        /// <param name="inputUrl">An URL string.</param>
        /// <returns>The URL string without spaces, doubled slashes and with a trailing slash.</returns>
        public static string SanitizeUrl(string inputUrl)
        {
            // Extract the schema part
            Match schemaMatch = Regex.Match(inputUrl, @"^(?<schema>https?://)");

            // Remove double slashes and spaces from the remaining part of the URL
            string cleanedPath = Regex.Replace(inputUrl.Substring(schemaMatch.Length), @"[/\s]+", "/");

            // Remove slashes at the start
            cleanedPath = cleanedPath.TrimStart('/');

            // Add a trailing slash if not already present
            cleanedPath = cleanedPath.TrimEnd('/') + "/";

            // Combine the schema and cleaned path
            string cleanedUrl = schemaMatch.Groups["schema"].Value + cleanedPath;

            return cleanedUrl;
        }

        /// <summary>
        /// Format episodes and series descriptions replacing newlines with br tags.
        /// </summary>
        /// <param name="description">String to format.</param>
        /// <returns>A string with \n replaced by br tags.</returns>
        public static string FormatDescription(string description)
        {
            if (description == null)
            {
                return "Description is null";
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
        /// Get video name from file path on the disk.
        /// </summary>
        /// <param name="path">File path on disk.</param>
        /// <returns>The video name.</returns>
        public static string GetVideoNameFromPath(string path)
        {
            return path.Split(DetectDirectorySeparator(path)).Last().Split(".").First();
        }

        /// <summary>
        /// Get channel name from directory path on the disk.
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
    }
}
