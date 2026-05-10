using System;
using System.Collections.Generic;
using System.Text;

namespace Solamirare;

internal static unsafe class HttpClientHelper
{
    // 用于修复路径解析的静态根字符
    private static readonly char _rootChar = '/';


    /// <summary>
    /// 解析 HTTP URL，提取 host、port、path。
    /// </summary>
    public static bool TryParseHttpUrl(
        UnManagedCollection<char> url,
        out char* hostPtr, out int hostLen,
        out ushort port,
        out char* pathPtr, out int pathLen)
    {
        hostPtr = null; hostLen = 0;
        port = 80;
        pathPtr = null; pathLen = 0;

        char* p = url.InternalPointer;
        if (p == null) return false;

        int n = (int)url.Size;
        if (n < 8) return false;

        int i = 0;
        if (url.StartsWith("https://"))
        {
            i = 8;
            port = 443;
        }
        else if (url.StartsWith("http://"))
        {
            i = 7;
            port = 80;
        }
        else return false;

        int hostStart = i;
        while (i < n && p[i] != ':' && p[i] != '/') i++;

        hostPtr = p + hostStart;
        hostLen = i - hostStart;

        if (i < n && p[i] == ':')
        {
            i++;
            int value = 0;
            while (i < n && p[i] >= '0' && p[i] <= '9')
            {
                if (value > 65535) return false;
                value = value * 10 + (p[i] - '0');
                i++;
            }
            if (value > 0 && value <= 65535)
                port = (ushort)value;
        }

        if (i < n && p[i] == '/')
        {
            pathPtr = p + i;
            pathLen = n - i;
        }
        else
        {
            fixed (char* r = &_rootChar)
            {
                pathPtr = r;
            }
            pathLen = 1;
        }

        return hostLen > 0;
    }

    public static bool ResolveHostMacOS(char* hostPtr, int hostLen, out uint addr)
    {
        addr = 0;
        if (hostLen <= 0 || hostLen > 255) return false;

        fixed (byte* bytes = new byte[hostLen + 1])
        {
            for (int i = 0; i < hostLen; i++)
                bytes[i] = (byte)hostPtr[i];
            bytes[hostLen] = 0;

            MacOSHttpPosixApi.hostent* h = MacOSAPI.gethostbyname(bytes);
            if (h == null || h->h_addr_list == null || h->h_addr_list[0] == null) return false;

            addr = *(uint*)h->h_addr_list[0];
        }
        return true;
    }

    /// <summary>
    /// 通过 gethostbyname 解析主机名为 IPv4 地址。
    /// </summary>
    public static bool ResolveHostLinux(char* hostPtr, int hostLen, out uint addr)
    {
        addr = 0;
        if (hostLen <= 0 || hostLen > 255) return false;

        byte* bytes = stackalloc byte[hostLen + 1];
        for (int i = 0; i < hostLen; i++)
            bytes[i] = (byte)hostPtr[i];
        bytes[hostLen] = 0;

        hostent* h = LinuxAPI.gethostbyname(bytes);
        if (h == null || h->h_addr_list == null || h->h_addr_list[0] == null)
            return false;

        addr = *(uint*)h->h_addr_list[0];
        return true;
    }

    public static bool ResolveHostWindows(char* hostPtr, int hostLen, out uint addr)
    {
        addr = 0;
        if (hostLen <= 0 || hostLen > 255) return false;

        byte* bytes = stackalloc byte[hostLen + 1];
        for (int i = 0; i < hostLen; i++)
            bytes[i] = (byte)hostPtr[i];
        bytes[hostLen] = 0;

        IntPtr hPtr = WindowsAPI.gethostbyname_raw(bytes);
        if (hPtr == IntPtr.Zero) return false;

        hostent* h = (hostent*)hPtr;
        if (h->h_addr_list == null || h->h_addr_list[0] == null) return false;

        addr = *(uint*)h->h_addr_list[0];
        return true;
    }


    /// <summary>
    /// 将 char* 字符串以 ASCII 编码写入字节缓冲区。
    /// </summary>
    public static int WriteAscii(byte* dst, int offset, char* src, int len)
    {
        if (offset + len > 1024) return 0;
        for (int i = 0; i < len; i++)
            dst[offset + i] = (byte)src[i];
        return len;
    }

    /// <summary>
    /// 将 string 字面量以 ASCII 编码写入字节缓冲区。
    /// </summary>
    public static int WriteAscii(byte* dst, int offset, ReadOnlySpan<char> text)
    {
        int len = text.Length;
        if (offset + len > 1024) return 0;

        for (int i = 0; i < len; i++)
            dst[offset + i] = (byte)text[i];
        return len;
    }

    public static int WriteAscii(byte* dst, int offset, int capacity, char* src, int len)
    {
        if (len > 0 && offset + len > capacity) return -1;
        for (int i = 0; i < len; i++)
            dst[offset + i] = (byte)src[i];
        return len;
    }

    public static int WriteAscii(byte* dst, int offset, int capacity, ReadOnlySpan<char> text)
    {
        int len = text.Length;
        if (len > 0 && offset + len > capacity) return -1;

        for (int i = 0; i < len; i++)
            dst[offset + i] = (byte)text[i];

        return len;
    }

    /// <summary>
    /// 将 char* 复制到 byte* 缓冲区。
    /// </summary>
    public static void CopyChars(char* src, byte* dst, uint len)
    {
        for (int i = 0; i < len; i++) dst[i] = (byte)src[i];
    }
}