using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TicketVoidingSystem.Utilities;

public class Gmt8DateTimeJsonConverter : JsonConverter<DateTime>
{
    private const int OffSetHours = 12;

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString() ?? string.Empty, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var customDateTime = value;
        customDateTime = customDateTime.AddHours(OffSetHours);
        writer.WriteStringValue(customDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    }
}