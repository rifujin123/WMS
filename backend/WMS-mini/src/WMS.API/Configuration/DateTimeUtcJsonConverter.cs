using System.Text.Json;
using System.Text.Json.Serialization;

namespace WMS.API.Configuration;

public class DateTimeUtcJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!).ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

public class NullableDateTimeUtcJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return string.IsNullOrEmpty(str) ? null : DateTime.Parse(str).ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            var utc = value.Value.Kind == DateTimeKind.Utc
                ? value.Value
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
