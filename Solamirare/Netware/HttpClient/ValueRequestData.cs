using System.Runtime.CompilerServices;

namespace Solamirare;


[SkipLocalsInit]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8, Size = 64)]
internal unsafe ref struct ValueRequestData
{

    public UnManagedCollection<char> url;

    public UnManagedMemory<byte>* responseBuffer;

    public UnManagedString* body;

    public ValueHttpResponse* result;

    public ValueDictionary<UnManagedString, UnManagedString>* headers;

    public int maxRetries;

    public int socketTimeoutSeconds;

    public bool disposed;


    public HttpMethod method;

    public HttpContentType contentType;

}