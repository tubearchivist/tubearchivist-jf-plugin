using System.Text.RegularExpressions;

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
            var maxLength = 500;
            if (Plugin.Instance != null)
            {
                maxLength = Plugin.Instance.Configuration.MaxDescriptionLength;
            }

            if (description.Length > maxLength)
            {
                description = description.Substring(0, maxLength);
                description = description.Replace("\n", "<br>", System.StringComparison.CurrentCulture);
            }

            return description;
        }
    }
}
