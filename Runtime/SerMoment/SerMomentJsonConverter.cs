#if HAS_NEWTONSOFT
using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace RiseOn.Serializables {
    [JsonConverter(typeof(SerMomentJsonConverter))]
    public partial struct SerMoment { }

    /// <summary>
    /// Writes a <see cref="SerMoment"/> as a bare number of Unix milliseconds.<br/>
    /// Reads that number, a date string in any form <see cref="SerMoment.TryParse"/> accepts, and the <c>{"unixMs": N}</c> form written without this converter.
    /// </summary>
    [Preserve]
    internal sealed class SerMomentJsonConverter : JsonConverter<SerMoment> {
        [Preserve]
        public SerMomentJsonConverter() { }

        public override void WriteJson(JsonWriter writer, SerMoment moment, JsonSerializer serializer) {
            writer.WriteValue(moment.unixMs);
        }

        public override SerMoment ReadJson(JsonReader reader, Type objectType, SerMoment existingValue, bool hasExistingValue, JsonSerializer serializer) {
            switch (reader.TokenType) {
                case JsonToken.Null:
                    return default;

                case JsonToken.Integer:
                case JsonToken.Float:
                    return new SerMoment(Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture));

                // The reader turns ISO 8601 strings into dates by itself unless DateParseHandling is None.
                // Which of the two types it hands over depends on DateParseHandling; anything else falls through
                // to the throw below instead of blowing up on the cast.
                case JsonToken.Date when reader.Value is DateTimeOffset dateTimeOffset:
                    return new SerMoment(dateTimeOffset);

                case JsonToken.Date when reader.Value is DateTime dateTime:
                    return new SerMoment(dateTime);

                case JsonToken.String:
                    var text = (string)reader.Value;
                    if (SerMoment.TryParse(text, out var moment)) return moment;

                    throw new JsonSerializationException($"Cannot read '{text}' as a {nameof(SerMoment)}.");

                case JsonToken.StartObject:
                    var json = JObject.Load(reader);
                    if (json.TryGetValue(nameof(SerMoment.unixMs), out var unixMs) && unixMs.Type is not JTokenType.Null) {
                        return new SerMoment(unixMs.Value<long>());
                    }

                    return default;

                default:
                    throw new JsonSerializationException($"Unexpected {reader.TokenType} when reading a {nameof(SerMoment)}.");
            }
        }
    }
}
#endif
