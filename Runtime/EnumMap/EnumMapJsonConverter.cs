#if HAS_NEWTONSOFT
using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace RiseOn.Serializables {
    [JsonConverter(typeof(EnumMapJsonConverter))]
    public partial class EnumMap<TKey, TValue> : IEnumMapJson {
        private const string KeyProperty   = nameof(EnumMapEntry<TKey, TValue>.key);
        private const string NameProperty  = nameof(EnumMapEntry<TKey, TValue>.name);
        private const string ValueProperty = nameof(EnumMapEntry<TKey, TValue>.value);

        void IEnumMapJson.WriteJson(JsonWriter writer, JsonSerializer serializer) {
            var keys          = EnumMapKeys<TKey>.Keys;
            var names         = EnumMapKeys<TKey>.Names;
            var numberType    = Enum.GetUnderlyingType(typeof(TKey));

            writer.WriteStartArray();
            for (int i = 0; i < keys.Length; ++i) {
                writer.WriteStartObject();
                writer.WritePropertyName(KeyProperty);
                writer.WriteValue(Convert.ChangeType(keys[i], numberType, CultureInfo.InvariantCulture));
                writer.WritePropertyName(NameProperty);
                writer.WriteValue(names[i]);
                writer.WritePropertyName(ValueProperty);
                serializer.Serialize(writer, ValueAt(i), typeof(TValue));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        void IEnumMapJson.ReadJson(JsonReader reader, JsonSerializer serializer) {
            entries.Clear();

            if (reader.TokenType is JsonToken.StartArray) {
                foreach (var token in JArray.Load(reader)) {
                    if (token is not JObject item) continue;

                    var name = item[NameProperty]?.Type is JTokenType.String ? (string)item[NameProperty] : null;
                    if (!TryReadKey(item[KeyProperty], name, out var key)) continue;

                    entries.Add(new EnumMapEntry<TKey, TValue>(key, name, ReadValue(item[ValueProperty], serializer)));
                }
            } else if (reader.TokenType is JsonToken.StartObject) {
                // {"Name": value}: what Newtonsoft writes for the map when this converter is not compiled.
                foreach (var property in JObject.Load(reader).Properties()) {
                    var ordinal = Array.IndexOf(EnumMapKeys<TKey>.Names, property.Name);
                    if (ordinal >= 0) {
                        entries.Add(new EnumMapEntry<TKey, TValue>(EnumMapKeys<TKey>.Keys[ordinal], property.Name, ReadValue(property.Value, serializer)));
                    } else if (long.TryParse(property.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) {
                        entries.Add(new EnumMapEntry<TKey, TValue>((TKey)Enum.ToObject(typeof(TKey), number), null, ReadValue(property.Value, serializer)));
                    }
                }
            } else {
                throw new JsonSerializationException($"RiseOn EnumMap: unexpected token {reader.TokenType}, expected an array or an object.");
            }

            entryIndices = null;
        }

        /// <summary>
        /// Reads the saved number of an entry. An entry without a readable number can still be matched by name.
        /// </summary>
        private static bool TryReadKey(JToken token, string name, out TKey key) {
            if (token is JValue { Type: JTokenType.Integer } number) {
                try {
                    key = (TKey)Enum.ToObject(typeof(TKey), number.Value);
                    return true;
                } catch (ArgumentException) { }
            }

            var ordinal = name == null ? -1 : Array.IndexOf(EnumMapKeys<TKey>.Names, name);
            key = ordinal >= 0 ? EnumMapKeys<TKey>.Keys[ordinal] : default;
            return ordinal >= 0;
        }

        private static TValue ReadValue(JToken token, JsonSerializer serializer) {
            return token == null ? default : (TValue)token.ToObject(typeof(TValue), serializer);
        }
    }

    internal interface IEnumMapJson {
        void WriteJson(JsonWriter writer, JsonSerializer serializer);
        void ReadJson(JsonReader reader, JsonSerializer serializer);
    }

    /// <summary>
    /// Writes an <see cref="EnumMap{TKey,TValue}"/> as its entries (number, name, value) and reads it back the same way Unity data is read: matched to the current enum by name, then by number.
    /// </summary>
    [Preserve]
    internal sealed class EnumMapJsonConverter : JsonConverter {
        [Preserve]
        public EnumMapJsonConverter() { }

        public override bool CanConvert(Type objectType) {
            return typeof(IEnumMapJson).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value is null) {
                writer.WriteNull();
                return;
            }

            ((IEnumMapJson)value).WriteJson(writer, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType is JsonToken.Null) return null;

            var map = (IEnumMapJson)Activator.CreateInstance(objectType);
            map.ReadJson(reader, serializer);
            return map;
        }
    }
}
#endif
