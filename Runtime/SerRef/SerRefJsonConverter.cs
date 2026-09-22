#if HAS_NEWTONSOFT
using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace RiseOn.Serializables {
    [JsonConverter(typeof(SerRefJsonConverter))]
    public partial struct SerRef<TValue> : ISerRefJson {
        void ISerRefJson.WriteJson(JsonWriter writer, JsonSerializer serializer) {
            serializer.Serialize(writer, value, typeof(TValue));
        }

        object ISerRefJson.ReadJson(JsonReader reader, JsonSerializer serializer) {
            return new SerRef<TValue>((TValue)serializer.Deserialize(reader, typeof(TValue)));
        }
    }

    internal interface ISerRefJson {
        void WriteJson(JsonWriter writer, JsonSerializer serializer);
        object ReadJson(JsonReader reader, JsonSerializer serializer);
    }

    /// <summary>
    /// Writes a <see cref="SerRef{TValue}"/> as the value it wraps: <c>{"reward": {...}}</c> rather than
    /// <c>{"reward": {"value": {...}}}</c>.
    /// </summary>
    [Preserve]
    internal sealed class SerRefJsonConverter : JsonConverter {
        [Preserve]
        public SerRefJsonConverter() { }

        public override bool CanConvert(Type objectType) => typeof(ISerRefJson).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            ((ISerRefJson)value).WriteJson(writer, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var serRef = (ISerRefJson)(existingValue ?? Activator.CreateInstance(objectType));
            return serRef.ReadJson(reader, serializer);
        }
    }
}
#endif
