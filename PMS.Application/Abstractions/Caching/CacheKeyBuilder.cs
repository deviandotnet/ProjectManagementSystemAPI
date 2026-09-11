using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PMS.Application.Abstractions.Caching;

public static class CacheKeyBuilder
{
    public const string Prefix = "pms:v1";

    public static string Create(string feature, params object?[] dimensions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);

        var builder = new StringBuilder(Prefix)
            .Append(':')
            .Append(Normalize(feature));

        foreach (object? dimension in dimensions)
        {
            builder.Append(':').Append(Format(dimension));
        }

        return builder.ToString();
    }

    public static string Fingerprint(params object?[] values)
    {
        string canonical = string.Join('|', values.Select(value =>
        {
            string formatted = FormatFingerprintValue(value);
            return $"{Encoding.UTF8.GetByteCount(formatted)}:{formatted}";
        }));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(hash);
    }

    public static int NormalizePageNumber(int pageNumber) => Math.Max(pageNumber, 1);

    public static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, 100);

    private static string Format(object? value) => value switch
    {
        null => "_",
        string text => Normalize(text),
        Guid guid => guid.ToString("N"),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        Enum enumValue => enumValue.ToString().ToLowerInvariant(),
        IEnumerable<int> numbers => string.Join(',', numbers.Distinct().Order()),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "_",
        _ => Normalize(value.ToString() ?? "_")
    };

    private static string FormatFingerprintValue(object? value) => value switch
    {
        null => "_",
        string text => text.Trim(),
        Guid guid => guid.ToString("N"),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        Enum enumValue => enumValue.ToString(),
        IEnumerable<int> numbers => string.Join(',', numbers.Distinct().Order()),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "_",
        _ => value.ToString()?.Trim() ?? "_"
    };

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
