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
    /// Writes a <see cref="SerMoment"/> as a bare number of Unix seconds, and still reads the
    /// <c>{"value": N}</c> form written without this converter.
    /// </summary>
    [Preserve]
    internal sealed class SerMomentJsonConverter : JsonConverter<SerMoment> {
        [Preserve]
        public SerMomentJsonConverter() { }

        public override void WriteJson(JsonWriter writer, SerMoment moment, JsonSerializer serializer) {
            writer.WriteValue(moment.value);
        }

        public override SerMoment ReadJson(JsonReader reader, Type objectType, SerMoment existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType is JsonToken.Null) return default;

            if (reader.TokenType is JsonToken.StartObject) {
                var value = JObject.Load(reader)[nameof(SerMoment.value)];
                return new SerMoment { value = value is null || value.Type is JTokenType.Null ? 0 : value.Value<long>() };
            }

            return new SerMoment { value = Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture) };
        }
    }
}
#endif
