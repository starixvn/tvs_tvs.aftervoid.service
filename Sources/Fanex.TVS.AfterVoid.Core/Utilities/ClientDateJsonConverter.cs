using System;
using Newtonsoft.Json;

namespace Fanex.TVS.AfterVoid.Core.Utilities
{
    public class ClientDateJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dateTime = (DateTime)value;
            dateTime = dateTime.AddHours(12);
            writer.WriteValue(dateTime);
        }
    }
}