using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kyna.Common;

public static class JsonOptionsRepository
{
    private static JsonSerializerOptions? _options;

    public static JsonSerializerOptions DefaultSerializerOptions
    {
        get
        {
            if (_options == null)
            {
                _options = new()
                {
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true
                };

                _options.Converters.Add(new NullableStringJsonConverter());

                _options.Converters.Add(new DateOnlyJsonConverter());
                _options.Converters.Add(new NullableDateOnlyJsonConverter());

                _options.Converters.Add(new DateTimeJsonConverter());
                _options.Converters.Add(new NullableDateTimeJsonConverter());

                _options.Converters.Add(new DoubleJsonConverter());
                _options.Converters.Add(new NullableDoubleJsonConverter());

                _options.Converters.Add(new DecimalJsonConverter());
                _options.Converters.Add(new NullableDecimalJsonConverter());

                _options.Converters.Add(new LongJsonConverter());
                _options.Converters.Add(new NullableLongJsonConverter());

                _options.Converters.Add(new BooleanJsonConverter());
                _options.Converters.Add(new NullableBooleanJsonConverter());
            }

            return _options;
        }
    }
}

public class NullableStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();

        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        return str;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}

/// <summary>
/// DateOnly Json Converter.
/// <seealso href="https://stackoverflow.com/questions/71021064/serialize-json-from-object-to-string-dateonly"/>
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string dateString = reader.GetString() ?? DateOnly.MinValue.ToString(Format);

        dateString = dateString == "0000-00-00" ? DateOnly.MinValue.ToString(Format) : dateString;

        return DateOnly.ParseExact(dateString, Format, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}

public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? dateString = reader.GetString();

        if (dateString == null)
        {
            return null;
        }
        else
        {
            // this is necessary to support eodhd.com's api - this is how they handle null dates.
            dateString = dateString == "0000-00-00" ? null : dateString;

            if (dateString == null)
            {
                return null;
            }
            return DateOnly.ParseExact(dateString, Format, CultureInfo.InvariantCulture);
        }
    }


    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? dateString = reader.GetString();

        if (dateString != null)
        {
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                return result;
            }
        }

        return DateTime.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("u"));
    }
}

public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? dateString = reader.GetString();

        // this is necessary to support eodhd.com's api - this is how they handle null dates.
        if (dateString == null || dateString == "0000-00-00 00:00:00")
        {
            return null;
        }
        else
        {
            if (DateTime.TryParseExact(dateString, Format, null, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime;
            }
            return null;
        }
    }


    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class LongJsonConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var val = reader.GetString();

            if (string.IsNullOrWhiteSpace(val))
            { return 0L; }

            if (long.TryParse(val, out long l))
            {
                return l;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class NullableLongJsonConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var val = reader.GetString();

            if (string.IsNullOrWhiteSpace(val))
            { return null; }

            if (long.TryParse(val, out long l))
            {
                return l;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class DoubleJsonConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var val = reader.GetString();

            if (string.IsNullOrWhiteSpace(val))
            { return 0D; }

            if (double.TryParse(val, out double d))
            {
                return d;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDouble();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class NullableDoubleJsonConverter : JsonConverter<double?>
{
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var val = reader.GetString();

            if (string.IsNullOrWhiteSpace(val))
            { return null; }

            if (double.TryParse(val, out double d))
            {
                return d;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDouble();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class DecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var val = reader.GetString();

            if (string.IsNullOrWhiteSpace(val))
            { return 0M; }

            if (decimal.TryParse(val, out decimal d))
            {
                return d;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class NullableDecimalJsonConverter : JsonConverter<decimal?>
{
    // Sometimes the values delivered by the eodhd API are exponential values (e.g., "1.0E+18").
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
    private static readonly Regex _notationRegex = new(@"([\d\.]+)?E\+(\d+)", RegexOptions.IgnoreCase);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? val = null;

        if (reader.TokenType == JsonTokenType.String)
        {
            val = reader.GetString();

            if (string.IsNullOrWhiteSpace(val))
            { return null; }

            if (_notationRegex.IsMatch(val))
            {
                var m = _notationRegex.Match(val);
                if (!decimal.TryParse(m.Groups[1].Value, out decimal b))
                { return null; }
                if (!double.TryParse(m.Groups[2].Value, out double numZeroes))
                { return null; }

                return b * (decimal)Math.Pow(10, numZeroes);
            }

            if (decimal.TryParse(val, out decimal d))
            {
                return d;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }

        throw new JsonException($"Could not parse value {val} as a nullable decimal.");
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class BooleanJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }
        else if (reader.TokenType == JsonTokenType.False)
        {
            return false;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var val = reader.GetString()?.ToLower();

            if (string.IsNullOrWhiteSpace(val))
            { return false; }

            if (bool.TryParse(val, out bool b))
            {
                return b;
            }

            if (val == "t" || val == "true" || val == "y" || val == "yes")
            { return true; }

            if (val == "f" || val == "false" || val == "n" || val == "no")
            { return false; }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var i = reader.GetInt32();
            return i != 0;
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

public class NullableBooleanJsonConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }
        else if (reader.TokenType == JsonTokenType.False)
        {
            return false;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var val = reader.GetString()?.ToLower();

            if (string.IsNullOrWhiteSpace(val))
            { return null; }

            if (bool.TryParse(val, out bool b))
            {
                return b;
            }

            if (val == "t" || val == "true" || val == "y" || val == "yes")
            { return true; }

            if (val == "f" || val == "false" || val == "n" || val == "no")
            { return false; }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var i = reader.GetInt32();
            return i != 0;
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteBooleanValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class EnumDescriptionConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for {typeof(T).FullName}.");
        }

        return reader.GetString()?.GetEnumValueFromDescription<T>() ?? default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.GetEnumDescription());
    }
}
