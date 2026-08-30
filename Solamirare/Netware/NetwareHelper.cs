namespace Solamirare;

/// <summary>
/// 网络相关常用
/// </summary>
public static class NetwareHelper
{

    /// <summary>
    /// 是否合法的 http 或者 https 地址。
    /// 
    /// <para> 接受：</para>
    /// <para>http://127.0.0.1          ✓</para>
    /// <para>http://127.0.0.1:8080     ✓</para>
    /// <para>https://127.0.0.1         ✓</para>
    /// <para>https://example.com:443   ✓</para>
    /// <para>https://[::1]:8080        ✓</para>
    /// 
    /// <para> 不接受：</para>
    /// <para>ftp://example.com         ✗</para>
    /// <para>ws://example.com          ✗</para>
    /// <para>example.com               ✗</para>
    /// <para>http:example.com           ✗</para>
    /// <para>https:example.com          ✗</para>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsValidHttpAddress(ReadOnlySpan<char> value)
    {
        // 必须以 http:// 或 https:// 开头
        if (value.StartsWith("http://"))
        {
            value = value[7..];
        }
        else if (value.StartsWith("https://"))
        {
            value = value[8..];
        }
        else
        {
            return false;
        }

        if (value.IsEmpty)
            return false;

        // IPv6：[::1]:8080
        if (value[0] == '[')
        {
            int close = value.IndexOf(']');

            if (close <= 1)
                return false;

            ReadOnlySpan<char> host = value[1..close];

            if (!IsValidIPv6(host))
                return false;

            value = value[(close + 1)..];

            // IPv6 后面没有 Port
            if (value.IsEmpty)
                return true;

            if (value[0] != ':')
                return false;

            return IsValidPort(value[1..]);
        }

        // 普通 hostname / IPv4
        int colon = value.IndexOf(':');

        if (colon >= 0)
        {
            ReadOnlySpan<char> host = value[..colon];
            ReadOnlySpan<char> port = value[(colon + 1)..];

            if (!IsValidHost(host))
                return false;

            return IsValidPort(port);
        }

        return IsValidHost(value);
    }

    private static bool IsValidPort(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return false;

        int port = 0;

        foreach (char c in value)
        {
            if ((uint) (c - '0') > 9)
                return false;

            port = port * 10 + (c - '0');

            if (port > 65535)
                return false;
        }

        return true;
    }

    private static bool IsValidHost(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return false;

        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c) ||
                c == '-' ||
                c == '.' ||
                c == '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsValidIPv6(ReadOnlySpan<char> value)
    {
        foreach (char c in value)
        {
            if (char.IsAsciiHexDigit(c) ||
                c == ':' ||
                c == '.')
            {
                continue;
            }

            return false;
        }

        return true;
    }



    //=============================

    /// <summary>
    /// 获取端口号
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static int GetPort(ReadOnlySpan<char> address)
    {
        bool https = false;

        if (address.StartsWith("https://"))
        {
            address = address[8..];
            https = true;
        }
        else if (address.StartsWith("http://"))
        {
            address = address[7..];
        }

        if (address.IsEmpty)
            return -1;

        // IPv6：[::1] 或 [::1]:8080
        if (address[0] == '[')
        {
            int close = address.IndexOf(']');

            if (close < 0)
                return -1;

            // [::1]
            if (close + 1 == address.Length)
                return https ? 443 : 80;

            // [::1] 后面必须是 :
            if (address[close + 1] != ':')
                return -1;

            return ParsePort(address[(close + 2)..]);
        }

        // IPv4 / 域名
        int colon = address.LastIndexOf(':');

        // 没有明确 Port
        if (colon < 0)
            return https ? 443 : 80;

        return ParsePort(address[(colon + 1)..]);
    }

    private static int ParsePort(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return -1;

        int port = 0;

        foreach (char c in value)
        {
            if ((uint) (c - '0') > 9)
                return -1;

            port = port * 10 + (c - '0');

            if (port > 65535)
                return -1;
        }

        return port;
    }

    //==============================================

    /// <summary>
    /// 从网络地址中提取主机部分，同时支持 IPV4 与 IPV6
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static ReadOnlySpan<char> GetHost(ReadOnlySpan<char> address)
    {
        if (address.StartsWith("https://"))
        {
            address = address[8..];
        }
        else if (address.StartsWith("http://"))
        {
            address = address[7..];
        }

        if (address.IsEmpty)
            return default;

        // IPv6：[::1] 或 [2001:db8::1]:8080
        if (address[0] == '[')
        {
            int close = address.IndexOf(']');

            if (close <= 1)
                return default;

            return address[1..close];
        }

        // 普通 IPv4 / 主机名
        int colon = address.IndexOf(':');

        if (colon >= 0)
            return address[..colon];

        return address;
    }

}