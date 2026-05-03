using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Solamirare;

/// <summary>
/// 提供基于 <see cref="Utf8JsonReader"/> 的 JSON 合法性校验功能。
/// </summary>
public static unsafe class JsonValidator
{
    /// <summary>
    /// 校验指定字符序列是否为合法 JSON。
    /// </summary>
    /// <param name="json">待校验的 JSON 字符数据。</param>
    [SkipLocalsInit]
    public static bool IsValidJson(ReadOnlySpan<char> json)
    {
        if (json.IsEmpty) return false;

        ReadOnlySpan<char> trimmed = json.TrimStart();

        if (trimmed.IsEmpty) return false;

        char first = trimmed[0];

        if (first != '{' && first != '[') return false;

        int byteCount = Encoding.UTF8.GetByteCount(json);

        if (byteCount <= 2048)
        {
            byte* buffer = stackalloc byte[byteCount];
            return ValidateCore(json, buffer, byteCount);
        }
        else
        {
            byte* buffer = (byte*)NativeMemory.Alloc((nuint)byteCount);

            try
            {
                return ValidateCore(json, buffer, byteCount);
            }
            finally
            {
                NativeMemory.Free(buffer);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ValidateCore(ReadOnlySpan<char> json, byte* buffer, int byteCount)
    {
        Span<byte> utf8Span = new Span<byte>(buffer, byteCount);

        Encoding.UTF8.GetBytes(json, utf8Span);

        var options = new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Skip
        };

        Utf8JsonReader reader = new Utf8JsonReader(utf8Span, options);

        try
        {
            while (reader.Read())
            {
            }

            return reader.BytesConsumed == utf8Span.Length;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
