using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// Custom JsonConverter to handle tags list that could be null.
    /// </summary>
    public class TagsJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Collection<string>);
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            var tags = new Collection<string>();

            if (token.Type == JTokenType.Array)
            {
                var stringArray = token.ToObject<string[]>();
                if (stringArray != null)
                {
                    foreach (var tag in stringArray)
                    {
                        tags.Add(tag);
                    }
                }
            }

            return tags;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var tags = (Collection<string>)(value ?? Array.Empty<string>());

            writer.WriteStartArray();
            foreach (var tag in tags)
            {
                writer.WriteValue(tag);
            }

            writer.WriteEndArray();
        }
    }
}
