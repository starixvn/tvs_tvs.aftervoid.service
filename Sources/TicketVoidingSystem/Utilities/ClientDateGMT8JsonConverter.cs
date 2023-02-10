using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace TicketVoidingSystem.Utilities;

[ExcludeFromCodeCoverage]
public class ClientDateGMT8JsonConverter : JsonConverter
{
    private const int OffsetHours = 12;

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
        dateTime = dateTime.AddHours(OffsetHours);
        writer.WriteValue(dateTime);
    }
}