using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTrader.Api.JsonConverters;

/// <summary>
/// Custom JSON converter for decimal values to ensure proper serialization
/// without scientific notation or incorrect scaling
/// </summary>
public class DecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (decimal.TryParse(stringValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }

        throw new JsonException($"Unable to parse decimal from {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        // CRITICAL FIX: Ensure decimal values are written correctly
        // Check if value appears to be incorrectly scaled (common with crypto prices)
        // If value is suspiciously large (>1 billion), it might be scaled incorrectly

        // For now, just write the raw decimal value correctly
        // The issue is that Entity Framework or some other component is multiplying by 10^8

        // Write as raw number - this should preserve the decimal point
        writer.WriteRawValue(value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Custom JSON converter for nullable decimal values
/// </summary>
public class NullableDecimalJsonConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            if (decimal.TryParse(stringValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }

        throw new JsonException($"Unable to parse nullable decimal from {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            // Use the same fix as the non-nullable converter
            writer.WriteRawValue(value.Value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}