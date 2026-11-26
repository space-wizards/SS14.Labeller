using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace SS14.Labeller;

/// <summary>
/// Writes Serilog events as newline-delimited compact JSON,
/// using the Serilog compact schema (@t, @m, @l, @x, @tr, @sp and the event's own properties).
/// Built only on <see cref="System.Text.Json"/>, so it stays compatible with Native AOT publishing.
/// REPLACE IT with default one when NAOT will be removed.
/// </summary>
public sealed class SerilogNAOTSafeJsonConsoleFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            writer.WriteString("Timestamp", logEvent.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteString("MessageTemplate", logEvent.MessageTemplate.Render(logEvent.Properties, CultureInfo.InvariantCulture));
            writer.WriteString("Message", logEvent.RenderMessage());

            if (logEvent.Level != LogEventLevel.Information)
                writer.WriteString("Level", logEvent.Level.ToString());

            if (logEvent.Exception is { } exception)
                writer.WriteString("Exception", exception.ToString());

            if (logEvent.TraceId is { } traceId)
                writer.WriteString("TraceId", traceId.ToHexString());

            if (logEvent.SpanId is { } spanId)
                writer.WriteString("SpanId", spanId.ToHexString());

            foreach (var property in logEvent.Properties)
            {
                var name = property.Key;
                if (name.Length > 0 && name[0] == '@')
                    name = '@' + name; // Escape a leading '@' by doubling it, as in Serilog.Formatting.Compact.

                writer.WritePropertyName(name);
                WriteValue(writer, property.Value);
            }

            writer.WriteEndObject();
        }

        output.Write(Encoding.UTF8.GetString(buffer.WrittenSpan));
        output.WriteLine();
    }

    private static void WriteValue(Utf8JsonWriter writer, LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue scalarValue:
                WriteScalar(writer, scalarValue.Value);
                break;

            case SequenceValue sequenceValue:
                writer.WriteStartArray();
                foreach (var element in sequenceValue.Elements)
                    WriteValue(writer, element);
                writer.WriteEndArray();
                break;

            case StructureValue structureValue:
                writer.WriteStartObject();
                if (structureValue.TypeTag is { Length: > 0 } typeTag)
                    writer.WriteString("$type", typeTag);
                foreach (var property in structureValue.Properties)
                {
                    writer.WritePropertyName(property.Name);
                    WriteValue(writer, property.Value);
                }
                writer.WriteEndObject();
                break;

            case DictionaryValue dictionaryValue:
                writer.WriteStartObject();
                foreach (var element in dictionaryValue.Elements)
                {
                    writer.WritePropertyName((element.Key as ScalarValue)?.Value?.ToString() ?? element.Key.ToString());
                    WriteValue(writer, element.Value);
                }
                writer.WriteEndObject();
                break;

            default:
                writer.WriteNullValue();
                break;
        }
    }

    private static void WriteScalar(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case byte n:
                writer.WriteNumberValue(n);
                break;
            case sbyte n:
                writer.WriteNumberValue(n);
                break;
            case short n:
                writer.WriteNumberValue(n);
                break;
            case ushort n:
                writer.WriteNumberValue(n);
                break;
            case int n:
                writer.WriteNumberValue(n);
                break;
            case uint n:
                writer.WriteNumberValue(n);
                break;
            case long n:
                writer.WriteNumberValue(n);
                break;
            case ulong n:
                writer.WriteNumberValue(n);
                break;
            case float n:
                writer.WriteNumberValue(n);
                break;
            case double n:
                writer.WriteNumberValue(n);
                break;
            case decimal n:
                writer.WriteNumberValue(n);
                break;
            case DateTime dateTime:
                writer.WriteStringValue(dateTime);
                break;
            case DateTimeOffset dateTimeOffset:
                writer.WriteStringValue(dateTimeOffset);
                break;
            case Guid guid:
                writer.WriteStringValue(guid);
                break;
            case char ch:
                writer.WriteStringValue(ch.ToString());
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
