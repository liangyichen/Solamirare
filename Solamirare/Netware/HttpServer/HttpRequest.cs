using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;


namespace Solamirare;

/// <summary>
/// 表示一个 HTTP 请求的解析结果。
/// 所有字段均为零拷贝切片，直接引用调用方传入的原始读缓冲区，不产生任何字节复制或托管堆分配。
/// 作为 ref struct，其生命周期严格绑定在栈帧内，不可装箱或存储到堆上。
/// <para>
/// Represents the parsed result of an HTTP request.
/// All fields are zero-copy slices that reference the raw read buffer supplied by the caller —
/// no byte copying or managed heap allocation occurs.
/// As a ref struct, its lifetime is strictly bound to the enclosing stack frame;
/// it cannot be boxed or stored on the heap.
/// </para>
/// </summary>
[StructLayout(LayoutKind.Auto)]
public unsafe struct UHttpRequest
{
    // =========================================================================
    // 静态常量与静态 Header Key 字段
    // Static constants and static header key fields
    // =========================================================================

    /// <summary>
    /// CRLF 序列（\r\n），用于 HTTP 协议行尾分隔。
    /// <para>CRLF sequence (\r\n) used as HTTP line terminator.</para>
    /// </summary>
    private static UnManagedCollection<byte> CRLF = "\r\n"u8;

    /// <summary>
    /// 回车符 CR（0x0D）。
    /// <para>Carriage return byte CR (0x0D).</para>
    /// </summary>
    private const byte CR = (byte)'\r';

    /// <summary>
    /// 换行符 LF（0x0A）。
    /// <para>Line feed byte LF (0x0A).</para>
    /// </summary>
    private const byte LF = (byte)'\n';

    // 以下静态字段保存常见 Header 名称的字节序列，供 ProcessHeaderLine 做快速路径比较。
    // 静态字段在进程生命周期内只初始化一次，避免在每次请求处理时重复构造。
    //
    // The following static fields hold byte sequences of common header names for fast-path
    // comparison in ProcessHeaderLine. Static fields are initialized once per process lifetime,
    // avoiding repeated construction on every request.

    /// <summary>
    /// "Content-Type" Header 名称的字节序列，用于 Init 中判断表单 Content-Type。
    /// <para>"Content-Type" header name bytes, used in Init to detect form Content-Type.</para>
    /// </summary>
    static UnManagedCollection<byte> s_ContentType = "Content-Type"u8;

    /// <summary>
    /// "Host" Header 名称的字节序列，用于 ProcessHeaderLine 快速路径识别。
    /// <para>"Host" header name bytes, used for fast-path recognition in ProcessHeaderLine.</para>
    /// </summary>
    static UnManagedCollection<byte> s_Host = "Host"u8;

    /// <summary>
    /// "Connection" Header 名称的字节序列，用于 ProcessHeaderLine 快速路径识别。
    /// <para>"Connection" header name bytes, used for fast-path recognition in ProcessHeaderLine.</para>
    /// </summary>
    static UnManagedCollection<byte> s_Connection = "Connection"u8;

    /// <summary>
    /// "Content-Length" Header 名称的字节序列，用于 ProcessHeaderLine 快速路径识别。
    /// <para>"Content-Length" header name bytes, used for fast-path recognition in ProcessHeaderLine.</para>
    /// </summary>
    static UnManagedCollection<byte> s_ContentLength = "Content-Length"u8;

    /// <summary>
    /// "User-Agent" Header 名称的字节序列，用于 ProcessHeaderLine 快速路径识别。
    /// <para>"User-Agent" header name bytes, used for fast-path recognition in ProcessHeaderLine.</para>
    /// </summary>
    static UnManagedCollection<byte> s_UserAgent = "User-Agent"u8;

    /// <summary>
    /// "Cookie" Header 名称的字节序列，用于 ProcessHeaderLine 快速路径识别。
    /// <para>"Cookie" header name bytes, used for fast-path recognition in ProcessHeaderLine.</para>
    /// </summary>
    static UnManagedCollection<byte> s_Cookie = "Cookie"u8;

    static UnManagedCollection<byte> urlencodeChars = "application/x-www-form-urlencoded"u8;


    // =========================================================================
    // 公有实例字段
    // Public instance fields
    // =========================================================================

    /// <summary>
    /// 本次请求中 Cookie Header 的原始值切片，例如 "session=abc; token=xyz"。
    /// 由 <see cref="ProcessHeaderLine"/> 在识别到 Cookie Header 时赋值，
    /// 随后由 <see cref="ProcessHeaders"/> 末尾的 Cookie 解析块按 ';' 和 '=' 切分为
    /// <see cref="Cookies"/> 字典。若请求不携带 Cookie，则此字段为空。
    /// <para>
    /// Raw value slice of the Cookie header in the current request, e.g. "session=abc; token=xyz".
    /// Assigned by <see cref="ProcessHeaderLine"/> when the Cookie header is recognized, then
    /// split by ';' and '=' into the <see cref="Cookies"/> dictionary at the end of
    /// <see cref="ProcessHeaders"/>. Empty if the request carries no Cookie header.
    /// </para>
    /// </summary>
    public UnManagedCollection<byte> CookieHeader;

    /// <summary>
    /// 全部非快速路径 HTTP 标头的键值对字典。
    /// 键和值均为零拷贝切片，直接引用原始读缓冲区。
    /// Host / Connection / Content-Length / User-Agent / Cookie 这五个常见 Header
    /// 由快速路径直接赋值到对应字段，不进入此字典。
    /// <para>
    /// Key-value dictionary of all non-fast-path HTTP headers.
    /// Both key and value are zero-copy slices referencing the raw read buffer.
    /// The five common headers Host / Connection / Content-Length / User-Agent / Cookie
    /// are handled by the fast path and stored in their dedicated fields, not in this dictionary.
    /// </para>
    /// </summary>
    public ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>> Headers;

    /// <summary>
    /// 请求路径切片，包含 '?' 之前的部分，例如 "/api/user"。
    /// 零拷贝，直接引用原始读缓冲区。
    /// <para>
    /// Request path slice containing everything before '?', e.g. "/api/user".
    /// Zero-copy — references the raw read buffer directly.
    /// </para>
    /// </summary>
    public UnManagedCollection<byte> Path;

    /// <summary>
    /// HTTP 请求方法，例如 GET、POST、PUT 等。
    /// 由 <see cref="RequestLineParser"/> 通过字节序列比较赋值。
    /// <para>
    /// HTTP request method, e.g. GET, POST, PUT.
    /// Assigned by <see cref="RequestLineParser"/> via byte-sequence comparison.
    /// </para>
    /// </summary>
    public ConnectionMethod Method;

    /// <summary>
    /// 连接协议，例如 HTTP、HTTPS、FTP 等。
    /// 由 <see cref="RequestLineParser"/> 从请求行的协议段解析赋值。
    /// <para>
    /// Connection protocol, e.g. HTTP, HTTPS, FTP.
    /// Assigned by <see cref="RequestLineParser"/> from the protocol segment of the request line.
    /// </para>
    /// </summary>
    public ConnectionProtocol Protocol;

    /// <summary>
    /// User-Agent Header 的值切片。若请求不携带此 Header，则为空。
    /// <para>Value slice of the User-Agent header. Empty if the request carries no such header.</para>
    /// </summary>
    public UnManagedCollection<byte> UserAgentHeader;

    /// <summary>
    /// Host Header 的值切片。若请求不携带此 Header，则为空。
    /// <para>Value slice of the Host header. Empty if the request carries no such header.</para>
    /// </summary>
    public UnManagedCollection<byte> HostHeader;

    /// <summary>
    /// Connection Header 的值切片，例如 "keep-alive" 或 "close"。若请求不携带此 Header，则为空。
    /// <para>Value slice of the Connection header, e.g. "keep-alive" or "close". Empty if absent.</para>
    /// </summary>
    public UnManagedCollection<byte> ConnectionHeader;

    /// <summary>
    /// Content-Length Header 的值切片，表示请求体的字节长度（原始字节形式，未解析为整数）。
    /// 若请求不携带此 Header，则为空。
    /// <para>
    /// Value slice of the Content-Length header, representing the body byte count in raw byte form
    /// (not yet parsed to an integer). Empty if the request carries no such header.
    /// </para>
    /// </summary>
    public UnManagedCollection<byte> ContentLengthHeader;

    /// <summary>
    /// URL 中 '?' 之后的查询参数原始字符串切片，例如 "k=0&amp;h=233"。
    /// 零拷贝，直接引用 Path 所在的原始读缓冲区。若 Path 不含 '?'，则为空。
    /// <para>
    /// Raw query string slice after '?' in the URL, e.g. "k=0&amp;h=233".
    /// Zero-copy — references the same raw read buffer as Path.
    /// Empty if Path contains no '?'.
    /// </para>
    /// </summary>
    public UnManagedCollection<byte> QueryString;

    /// <summary>
    /// POST 表单键值对字典。
    /// 键和值均为零拷贝切片，直接引用原始请求体缓冲区，不做 URL 解码。
    /// 解码由调用方在业务逻辑中按需调用 <c>UrlEncodeAndDecoder.DecodeToChars</c> 完成。
    /// 仅当 Content-Type 为 application/x-www-form-urlencoded 时填充，否则为空字典。
    /// <para>
    /// POST form key-value dictionary.
    /// Both key and value are zero-copy slices referencing the raw request body buffer, without URL decoding.
    /// Decoding is deferred to the caller via <c>UrlEncodeAndDecoder.DecodeToChars</c> as needed.
    /// Populated only when Content-Type is application/x-www-form-urlencoded; otherwise empty.
    /// </para>
    /// </summary>
    public ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>> Form;

    /// <summary>
    /// URL 查询参数键值对字典，由 <see cref="QueryString"/> 按 '&amp;' 和 '=' 切分而来。
    /// 键和值均为零拷贝切片，不做 URL 解码，解码由调用方按需完成。
    /// <para>
    /// URL query parameter key-value dictionary, produced by splitting <see cref="QueryString"/>
    /// by '&amp;' and '='. Both key and value are zero-copy slices without URL decoding;
    /// decoding is left to the caller as needed.
    /// </para>
    /// </summary>
    public ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>> Query;

    /// <summary>
    /// Cookie 键值对字典，由 CookieHeader 按 ';' 和 '=' 切分而来。
    /// 键和值均为零拷贝切片。若请求不携带 Cookie，则为空字典。
    /// <para>
    /// Cookie key-value dictionary, produced by splitting <see cref="CookieHeader"/>
    /// by ';' and '='. Both key and value are zero-copy slices.
    /// Empty if the request carries no Cookie header.
    /// </para>
    /// </summary>
    public ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>> Cookies;

    /// <summary>
    /// HTTP 协议版本，例如 Http11（HTTP/1.1）、Http20（HTTP/2.0）。
    /// 由 <see cref="RequestLineParser"/> 通过字节序列直接比较赋值，无浮点解析开销。
    /// <para>
    /// HTTP protocol version, e.g. Http11 (HTTP/1.1), Http20 (HTTP/2.0).
    /// Assigned by <see cref="RequestLineParser"/> via direct byte-sequence comparison,
    /// with no floating-point parsing overhead.
    /// </para>
    /// </summary>
    public HttpVersion HttpVersion { get; private set; }

    /// <summary>
    /// 标记当前实例是否已执行 <see cref="Dispose"/>，防止重复释放非托管资源。
    /// <para>
    /// Tracks whether <see cref="Dispose"/> has been called on this instance,
    /// preventing double-free of unmanaged resources.
    /// </para>
    /// </summary>
    bool _disposed;




    // =========================================================================
    // 公有方法
    // Public methods
    // =========================================================================

    /// <summary>
    /// 初始化（或复用）本实例，对传入的三段原始字节数据依次完成解析。
    /// 调用前需确保各字典已通过 <see cref="Clear"/> 重置，或为首次使用（字典尚未创建）。
    /// <para>
    /// Initializes (or reuses) this instance by sequentially parsing the three supplied
    /// raw byte segments. Before calling, ensure all dictionaries have been reset via
    /// <see cref="Clear"/>, or that this is a first-time use (dictionaries not yet created).
    /// </para>
    /// </summary>
    /// <param name="requestLineBytes">
    /// 请求行字节段，例如 "GET /path?k=1 HTTP/1.1"。
    /// <para>Request line bytes, e.g. "GET /path?k=1 HTTP/1.1".</para>
    /// </param>
    /// <param name="headerBlockBytes">
    /// 请求头块字节段，包含所有 Header 行（不含请求行和空行分隔符）。
    /// <para>Header block bytes containing all header lines (excluding the request line and blank-line separator).</para>
    /// </param>
    /// <param name="bodyBytes">
    /// 请求体字节段。GET 等无体请求可传 null 或指向空集合的指针。
    /// <para>Request body bytes. Pass null or a pointer to an empty collection for body-less methods such as GET.</para>
    /// </param>
    public void Init(
        UnManagedCollection<byte>* requestLineBytes,
        UnManagedCollection<byte>* headerBlockBytes,
        UnManagedCollection<byte>* bodyBytes
        )
    {


        // Headers 字典惰性初始化：首次使用时分配，后续 keep-alive 请求复用已有内存。
        // Lazy initialization of the Headers dictionary: allocated on first use,
        // reused on subsequent keep-alive requests.
        if (!Headers.Created)
        {
            Headers = new ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>(8);
        }

        // 第一步：解析请求行，提取 Method / Path / Protocol / HttpVersion。
        // Step 1: parse the request line to extract Method / Path / Protocol / HttpVersion.
        RequestLineParser(requestLineBytes);

        // 第二步：单遍扫描 Header 块，填充 Headers 字典及各快速路径字段。
        // Step 2: single-pass scan of the header block to populate the Headers dictionary
        // and all fast-path header fields.
        ProcessHeaders(headerBlockBytes);

        // 第三步：从 Path 中切出 '?' 之后的 QueryString 片段（零拷贝）。
        // Step 3: slice the QueryString fragment after '?' from Path (zero-copy).
        QueryString = ExtractUrlParameters(Path);

        // 把 Path 截断到 '?' 之前，让调用方拿到的 Path 是纯路径
        int qm = Path.IndexOf((byte)'?');
        if (qm >= 0)
            Path = Path.Slice(0, (uint)qm);

        if (!QueryString.IsEmpty)
        {
            fixed (ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>* pQuery = &Query)
            fixed (UnManagedCollection<byte>* pQueryString = &QueryString)
            {
                if (!pQuery->Created)
                {
                    *pQuery = new ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>(16, true);
                }

                SplitByteMapToValueDictionary(pQueryString, (byte)'&', (byte)'=', pQuery);
            }
        }


        // 第五步：解析 POST 表单参数。
        // 仅当 Method 为 POST、body 非空且 Content-Type 为
        // application/x-www-form-urlencoded 时执行，其余情况直接跳过。
        // 与 Query 策略完全对称：按 '&' 和 '=' 做零拷贝切片，不做 URL 解码，
        // 解码由调用方在业务逻辑中按需调用 UrlEncodeAndDecoder.DecodeToChars 完成。
        //
        // Step 5: parse POST form parameters.
        // Only executed when Method is POST, body is non-empty, and Content-Type is
        // application/x-www-form-urlencoded; otherwise skipped entirely.
        // Fully symmetric with Query strategy: zero-copy slices split by '&' and '=',
        // no URL decoding — decoding is deferred to the caller via
        // UrlEncodeAndDecoder.DecodeToChars as needed.
        if (Method == ConnectionMethod.POST && bodyBytes is not null && bodyBytes->Size > 0)
        {
            UnManagedCollection<byte>* contentType = Headers.Index(s_ContentType);


            fixed (UnManagedCollection<byte>* p_urlencodeChars = &urlencodeChars)
                if (contentType is not null && ValueTypeHelper.StartWithIgnoreCase(contentType, p_urlencodeChars))
                {
                    fixed (ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>* pForm = &Form)
                    {
                        if (!pForm->Created)
                            *pForm = new ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>(16, true);

                        SplitByteMapToValueDictionary(bodyBytes, (byte)'&', (byte)'=', pForm);
                    }
                }
        }

        _disposed = false;
    }

    /// <summary>
    /// 重置本实例以供同一连接的下一次 HTTP 请求复用（keep-alive 场景）。
    /// 清空所有切片字段和字典槽位，但保留已分配的字典内部内存，避免重新分配。
    /// 同时将 <c>_disposed</c> 重置为 false，确保连接关闭时 <see cref="Dispose"/> 能正确执行。
    /// <para>
    /// Resets this instance for reuse by the next HTTP request on the same connection (keep-alive).
    /// Clears all slice fields and dictionary slots while retaining the internally allocated
    /// dictionary memory to avoid re-allocation. Also resets <c>_disposed</c> to false so that
    /// <see cref="Dispose"/> executes correctly when the connection is eventually closed.
    /// </para>
    /// </summary>
    public void Clear()
    {


        // 重置字典槽位控制字节（标记所有槽位为空），保留底层内存供下次请求复用。
        // Reset dictionary slot control bytes (mark all slots as empty) while retaining
        // the underlying memory for reuse by the next request.
        Headers.Clear();
        Query.Clear();
        Cookies.Clear();
        Form.Clear();

        // 将所有指向原始读缓冲区的切片字段清零，防止残留上一次请求的数据。
        // 注意：这些字段不拥有所指向的内存，Clear() 仅清零字段本身（指针置 null，长度置 0），
        // 不会释放任何内存。
        //
        // Zero out all slice fields that point into the raw read buffer, preventing
        // stale data from the previous request from leaking into the next one.
        // Note: these fields do not own the memory they point to; Clear() only zeroes
        // the field itself (pointer set to null, length set to 0) without freeing memory.
        Path.Clear();
        QueryString.Clear();
        HostHeader.Clear();
        ConnectionHeader.Clear();
        ContentLengthHeader.Clear();
        UserAgentHeader.Clear();
        CookieHeader.Clear();

        // 将枚举字段重置为默认值，避免本次请求的方法/协议/版本被下次请求误读。
        // Reset enum fields to defaults to prevent this request's method/protocol/version
        // from being misread by the next request.
        Method = ConnectionMethod.UnKnown;
        Protocol = ConnectionProtocol.UnKnown;
        HttpVersion = HttpVersion.HTTP10;

        // 重置释放标志，确保连接关闭时 Dispose() 能正确执行。
        // Reset the disposal flag so that Dispose() executes correctly on connection close.
        _disposed = false;
    }

    /// <summary>
    /// 释放本实例持有的所有非托管资源（即各字典的哈希槽位内存）。
    /// 应在连接关闭时调用，而非在每次请求结束时调用（keep-alive 场景下应调用 <see cref="Clear"/>）。
    /// 若已调用过 Dispose，则直接返回 false，防止重复释放。
    /// <para>
    /// Releases all unmanaged resources owned by this instance (i.e., the hash-slot memory
    /// of each dictionary). Should be called when the connection closes, not at the end of
    /// each request (use <see cref="Clear"/> for keep-alive reuse instead).
    /// Returns false immediately if Dispose has already been called, preventing double-free.
    /// </para>
    /// </summary>
    /// <returns>
    /// 成功释放返回 true；若已释放则返回 false。
    /// <para>Returns true if resources were successfully released; false if already disposed.</para>
    /// </returns>
    public bool Dispose()
    {
        if (_disposed)
            return false;


        // 依次释放各字典的非托管内存。
        // Form / Query / Cookies / Headers 的 key 和 value 均为零拷贝切片，
        // 不持有独立的堆分配内存，因此只需 Dispose 字典本身的槽位内存即可。
        //
        // Release the unmanaged memory of each dictionary in turn.
        // The key and value of Form / Query / Cookies / Headers are all zero-copy slices
        // that do not own independently heap-allocated memory, so only the dictionary's
        // own slot memory needs to be released.
        Headers.Dispose();
        Query.Dispose();
        Cookies.Dispose();
        Form.Dispose();

        _disposed = true;
        return true;
    }


    /// <summary>
    /// 描述当前对象状态的哈希码。
    /// </summary>
    public ulong StatusCode
    {
        get
        {
            fixed (UHttpRequest* p = &this)
            {
                ulong result = Fingerprint.MemoryFingerprint(p);

                return result;
            }
        }
    }


    // =========================================================================
    // 私有解析方法
    // Private parsing methods
    // =========================================================================

    /// <summary>
    /// 解析 HTTP 请求行，提取 Method、Path、Protocol 和 HttpVersion 四个字段。
    /// 请求行格式为 "METHOD PATH PROTOCOL/VERSION"（ASCII，以空格分隔），
    /// 所有结果均为对原始缓冲区的零拷贝切片或枚举值，不产生任何内存分配。
    /// <para>
    /// Parses the HTTP request line and extracts the Method, Path, Protocol, and HttpVersion fields.
    /// The request line format is "METHOD PATH PROTOCOL/VERSION" (ASCII, space-delimited).
    /// All results are either zero-copy slices of the raw buffer or enum values;
    /// no memory allocation occurs.
    /// </para>
    /// </summary>
    /// <param name="line">指向请求行字节序列的指针。<para>Pointer to the request line byte sequence.</para></param>
    /// <returns>
    /// 解析成功返回 true；格式不合法（缺少空格或斜杠）返回 false。
    /// <para>Returns true on success; false if the line is malformed (missing space or slash).</para>
    /// </returns>
    bool RequestLineParser(UnManagedCollection<byte>* line)
    {
        // 通过两次 IndexOf(' ') 将请求行分解为三个区段：
        //   s1 之前         : METHOD（如 "GET"）
        //   s1+1 到 s2 之前 : PATH  （如 "/api/user?k=1"）
        //   s2+1 之后       : PROTOCOL/VERSION（如 "HTTP/1.1"）
        //
        // Decompose the request line into three segments using two IndexOf(' ') calls:
        //   before s1        : METHOD   (e.g. "GET")
        //   s1+1 to s2       : PATH     (e.g. "/api/user?k=1")
        //   after s2+1       : PROTOCOL/VERSION (e.g. "HTTP/1.1")
        int s1 = line->IndexOf((byte)' ');
        if (s1 <= 0) return false;

        UnManagedCollection<byte> remainder = line->Slice((uint)s1 + 1);

        int s2 = remainder.IndexOf((byte)' ');
        if (s2 <= 0) return false;

        UnManagedCollection<byte> protocolAndVersion = remainder.Slice((uint)s2 + 1);

        // 再通过 '/' 将 PROTOCOL/VERSION 分为协议名和版本号两段。
        // Further split PROTOCOL/VERSION at '/' into protocol name and version string.
        int slash = protocolAndVersion.IndexOf((byte)'/');
        if (slash <= 0) return false;

        // ── Method 识别：直接字节序列比较，无字符串分配 ──
        // Method recognition: direct byte-sequence comparison, no string allocation.
        UnManagedCollection<byte> methodSpan = line->Slice(0, (uint)s1);

        switch (methodSpan.Size)
        {
            case 3:
                if (methodSpan.InternalPointer[0] == (byte)'G')  // 长度3，首字符是 G，那么一定是 GET
                {
                    Method = ConnectionMethod.GET;
                }
                break;
            case 4:
                if (methodSpan.Equals("POST"u8))
                    Method = ConnectionMethod.POST;
                break;
            default:
                if (methodSpan.Equals("PUT"u8)) Method = ConnectionMethod.PUT;
                else if (methodSpan.Equals("DELETE"u8)) Method = ConnectionMethod.DELETE;
                else if (methodSpan.Equals("HEAD"u8)) Method = ConnectionMethod.HEAD;
                else if (methodSpan.Equals("OPTIONS"u8)) Method = ConnectionMethod.OPTIONS;
                else if (methodSpan.Equals("PATCH"u8)) Method = ConnectionMethod.PATCH;
                else if (methodSpan.Equals("CONNECT"u8)) Method = ConnectionMethod.CONNECT;
                else if (methodSpan.Equals("TRACE"u8)) Method = ConnectionMethod.TRACE;
                else if (methodSpan.Equals("PUSH"u8)) Method = ConnectionMethod.PUSH;
                else Method = ConnectionMethod.UnKnown;
                break;
        }


        // ── Protocol 识别 ──
        // Protocol recognition.
        UnManagedCollection<byte> protocolSpan = protocolAndVersion.Slice(0, (uint)slash);

        switch (protocolSpan.Size)
        {
            case 4:

                if (protocolSpan.InternalPointer[0] == (byte)'H') //一定是 HTTP 
                    Protocol = ConnectionProtocol.HTTP;

                break;
            case 5:

                Protocol = ConnectionProtocol.HTTPS; //唯一的五个字 

                break;
            default:

                if (protocolSpan.Equals("UDP"u8)) Protocol = ConnectionProtocol.UDP;
                else if (protocolSpan.Equals("TCP"u8)) Protocol = ConnectionProtocol.TCP;
                else if (protocolSpan.Equals("FTP"u8)) Protocol = ConnectionProtocol.FTP;
                else if (protocolSpan.Equals("SFTP"u8)) Protocol = ConnectionProtocol.SFTP;
                else Protocol = ConnectionProtocol.UnKnown;

                break;
        }


        // ── Path：零拷贝切片，不含查询参数 ──
        // Path: zero-copy slice, does not include query parameters.
        Path = remainder.Slice(0, (uint)s2);

        // ── Version：直接字节比较，避免 float.TryParse 的通用解析开销 ──
        // Version: direct byte comparison, avoids the general-purpose overhead of float.TryParse.
        UnManagedCollection<byte> versionSpan = protocolAndVersion.Slice((uint)slash + 1);

        if (versionSpan.Equals("1.1"u8)) HttpVersion = HttpVersion.HTTP11;
        else if (versionSpan.Equals("2.0"u8)) HttpVersion = HttpVersion.HTTP20;
        else if (versionSpan.Equals("3.0"u8)) HttpVersion = HttpVersion.HTTP30;
        else HttpVersion = HttpVersion.HTTP10;

        return true;
    }

    /// <summary>
    /// 从完整请求路径中提取 '?' 之后的查询参数字符串片段。
    /// 结果为对原始读缓冲区的零拷贝切片，不产生任何内存分配。
    /// <para>
    /// Extracts the query string fragment after '?' from a full request path.
    /// The result is a zero-copy slice of the raw read buffer; no memory allocation occurs.
    /// </para>
    /// </summary>
    /// <param name="requestPath">
    /// 完整请求路径，例如 "/path?k=0&amp;h=233" 或 "/path"。
    /// <para>Full request path, e.g. "/path?k=0&amp;h=233" or "/path".</para>
    /// </param>
    /// <returns>
    /// '?' 之后的字节片段（如 "k=0&amp;h=233"）；若路径不含 '?' 或 '?' 为末尾字符则返回空集合。
    /// <para>Byte slice after '?' (e.g. "k=0&amp;h=233"); empty if Path contains no '?' or '?' is the last character.</para>
    /// </returns>
    UnManagedCollection<byte> ExtractUrlParameters(UnManagedCollection<byte> requestPath)
    {
        if (requestPath.IsEmpty)
            return UnManagedCollection<byte>.Empty;

        int questionMarkIndex = requestPath.IndexOf((byte)'?');

        if (questionMarkIndex == -1)
        {
            // 路径不含查询参数，例如 "/api/user"。
            // Path contains no query parameters, e.g. "/api/user".
            return UnManagedCollection<byte>.Empty;
        }

        if (questionMarkIndex + 1 >= requestPath.Size)
        {
            // '?' 是末尾字符，例如 "/path?"，视为无参数。
            // '?' is the last character, e.g. "/path?"; treated as no parameters.
            return UnManagedCollection<byte>.Empty;
        }

        // 切出 '?' 之后的所有字节，零拷贝。
        // Slice everything after '?', zero-copy.
        return requestPath.Slice((uint)questionMarkIndex + 1);
    }


    static Vector256<byte> vColon256 = Vector256.IsHardwareAccelerated ? Vector256.Create((byte)':') : default;
    static Vector256<byte> vLF256 = Vector256.IsHardwareAccelerated ? Vector256.Create((byte)'\n') : default;
    static Vector128<byte> vColon128 = Vector128.IsHardwareAccelerated ? Vector128.Create((byte)':') : default;
    static Vector128<byte> vLF128 = Vector128.IsHardwareAccelerated ? Vector128.Create((byte)'\n') : default;



    /// <summary>
    /// 单遍扫描整个 Header 块字节序列，解析出所有 HTTP 标头。
    /// <br/>
    /// 工作原理：使用融合 SIMD 的状态机，每次迭代从当前扫描位置出发，
    /// 在 AVX2（32 字节）或 SSE2/NEON（16 字节）通道中并行搜索 ':' 和 '\n'，
    /// 找到最近的特殊字符后跳转处理：
    /// <list type="bullet">
    ///   <item><description>遇到 ':'：记录 keyEnd 和 valueStart（仅处理行内第一个冒号）。</description></item>
    ///   <item><description>遇到 '\n'：去除末尾 '\r'，调用 <see cref="ProcessHeaderLine"/> 处理完整的一行 Header，
    ///   然后将扫描指针推进到下一行行首。遇到空行则终止解析。</description></item>
    /// </list>
    /// 循环结束后补充处理最后一个未被 '\n' 终止的 Header（若存在）。
    /// 最后若 CookieHeader 非空，则调用 <see cref="SplitByteMapToValueDictionary"/> 解析 Cookie 键值对。
    /// <para>
    /// Single-pass scan of the entire header block byte sequence to parse all HTTP headers.
    /// How it works: a SIMD-fused state machine searches for ':' and '\n' in parallel within
    /// each iteration using AVX2 (32-byte) or SSE2/NEON (16-byte) lanes starting from the
    /// current scan position, then dispatches on the nearest special character found:
    /// <list type="bullet">
    ///   <item><description>On ':': records keyEnd and valueStart (only the first colon per line is processed).</description></item>
    ///   <item><description>On '\n': strips trailing '\r', calls <see cref="ProcessHeaderLine"/> for the complete
    ///   header line, then advances the scan pointer to the next line start. Terminates on an empty line.</description></item>
    /// </list>
    /// After the loop, handles the last header line not terminated by '\n' (if any).
    /// Finally, if CookieHeader is non-empty, calls <see cref="SplitByteMapToValueDictionary"/>
    /// to parse Cookie key-value pairs.
    /// </para>
    /// </summary>
    /// <param name="headerBlockBytes">
    /// 指向 Header 块字节序列的指针。
    /// <para>Pointer to the header block byte sequence.</para>
    /// </param>
    void ProcessHeaders(UnManagedCollection<byte>* headerBlockBytes)
    {
        // 如果 Header 块为空，直接返回。
        if (headerBlockBytes is null || headerBlockBytes->IsEmpty) return;

        byte* start = headerBlockBytes->InternalPointer;
        byte* end = start + headerBlockBytes->Size;

        // ── 第一阶段：SIMD 批量预扫描 ───────────────────────────────────────────
        // 用一个不间断的 SIMD 循环，一次性扫描整个 headerBlockBytes，找出所有 '\n' 的位置。
        // 结果存入栈分配的临时数组，避免堆分配。
        //
        // Phase 1: SIMD Bulk Pre-scanning
        // A single, uninterrupted SIMD loop scans the entire headerBlockBytes to find all '\n' positions.
        // Results are stored in a stack-allocated temporary array to avoid heap allocation.

        const int MaxHeaders = 128; // 假设最多 128 个 Header，足以覆盖绝大多数情况
        int* lfIndices = stackalloc int[MaxHeaders];
        int lfCount = 0;
        byte* currentPtr = start;

        // 替换后
        // 三个分支改为 if/else if/else，确保每段内存只被扫描一次
        while (currentPtr < end && lfCount < MaxHeaders)
        {
            int remaining = (int)(end - currentPtr);

            // AVX2 (32 字节)
            if (Vector256.IsHardwareAccelerated && remaining >= Vector256<byte>.Count)
            {
                int limit = remaining - (remaining % Vector256<byte>.Count);
                for (int i = 0; i < limit; i += Vector256<byte>.Count)
                {
                    uint mask = Vector256.Equals(Vector256.Load(currentPtr + i), vLF256).ExtractMostSignificantBits();
                    while (mask != 0)
                    {
                        int offset = BitOperations.TrailingZeroCount(mask);
                        lfIndices[lfCount++] = (int)(currentPtr - start) + i + offset;
                        if (lfCount >= MaxHeaders) goto EndScan;
                        mask &= mask - 1;
                    }
                }
                currentPtr += limit;
            }
            // SSE2/NEON (16 字节)：仅在 AVX2 不可用或剩余字节不足 32 时执行
            else if (Vector128.IsHardwareAccelerated && remaining >= Vector128<byte>.Count)
            {
                int limit = remaining - (remaining % Vector128<byte>.Count);
                for (int i = 0; i < limit; i += Vector128<byte>.Count)
                {
                    uint mask = Vector128.Equals(Vector128.Load(currentPtr + i), vLF128).ExtractMostSignificantBits();
                    while (mask != 0)
                    {
                        int offset = BitOperations.TrailingZeroCount(mask);
                        lfIndices[lfCount++] = (int)(currentPtr - start) + i + offset;
                        if (lfCount >= MaxHeaders) goto EndScan;
                        mask &= mask - 1;
                    }
                }
                currentPtr += limit;
            }
            // 标量回退：仅在 SIMD 不可用或剩余字节不足向量宽度时执行
            else
            {
                while (currentPtr < end)
                {
                    if (*currentPtr == '\n')
                    {
                        lfIndices[lfCount++] = (int)(currentPtr - start);
                        if (lfCount >= MaxHeaders) goto EndScan;
                    }
                    currentPtr++;
                }
            }
        }
    EndScan:

        // ── 第二阶段：标量代码精准收割 ───────────────────────────────────────────
        // 遍历第一阶段找到的换行符索引，逐行处理 Header。
        //
        // Phase 2: Precise Scalar Harvesting
        // Iterate through the newline indices found in Phase 1 to process headers line by line.
        byte* lineStart = start;
        for (int i = 0; i < lfCount; i++)
        {
            byte* lineEnd = start + lfIndices[i];
            if (lineEnd > lineStart && *(lineEnd - 1) == '\r') lineEnd--;

            // 空行标志着 Header 块结束
            if (lineEnd == lineStart) break;

            UnManagedCollection<byte> line = new UnManagedCollection<byte>(lineStart, (uint)(lineEnd - lineStart));
            int colonIndex = line.IndexOf((byte)':');

            if (colonIndex > 0)
            {
                ProcessHeaderLine(lineStart, lineStart + colonIndex, lineStart + colonIndex + 1, lineEnd);
            }

            lineStart = start + lfIndices[i] + 1;
        }

        // Cookie 解析：仅当本次请求携带了 Cookie Header 时才进行键值对拆分。
        // ProcessHeaderLine 已将 Cookie Header 的值切片存入 CookieHeader 字段；
        // 此处按 ';' 和 '=' 切分，填充 Cookies 字典。
        //
        // Cookie parsing: only split into key-value pairs when the current request carries
        // a Cookie header. ProcessHeaderLine has already stored the Cookie header value slice
        // in CookieHeader; here it is split by ';' and '=' to populate the Cookies dictionary.
        if (!CookieHeader.IsEmpty)
        {
            fixed (UnManagedCollection<byte>* pCookieHeader = &CookieHeader)
            fixed (ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>* pCookies = &Cookies)
            {
                if (!pCookies->Created)
                {
                    *pCookies = new ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>(16, true);
                }

                SplitByteMapToValueDictionary(pCookieHeader, (byte)';', (byte)'=', pCookies);
            }
        }
    }

    /// <summary>
    /// 处理 Header 块中的单行标头：跳过 value 前导空白，通过快速路径识别五个常见 Header
    /// （Host / Connection / Content-Length / User-Agent / Cookie），
    /// 其余标头存入 <see cref="Headers"/> 字典。
    /// 所有结果均为零拷贝切片，直接引用原始读缓冲区，不产生内存分配。
    /// <para>
    /// Processes a single header line from the header block: trims leading whitespace from
    /// the value, applies fast-path recognition for five common headers
    /// (Host / Connection / Content-Length / User-Agent / Cookie),
    /// and inserts all remaining headers into the <see cref="Headers"/> dictionary.
    /// All results are zero-copy slices referencing the raw read buffer; no allocation occurs.
    /// </para>
    /// </summary>
    /// <param name="keyStart">Header 名称起始指针。<para>Pointer to the start of the header name.</para></param>
    /// <param name="keyEnd">Header 名称结束指针（不含 ':'）。<para>Pointer past the end of the header name (exclusive of ':').</para></param>
    /// <param name="valueStart">Header 值起始指针（紧跟 ':' 之后，可能含前导空白）。<para>Pointer to the start of the header value (immediately after ':', may have leading whitespace).</para></param>
    /// <param name="valueEnd">Header 值结束指针（不含 '\r' 或 '\n'）。<para>Pointer past the end of the header value (exclusive of '\r' or '\n').</para></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ProcessHeaderLine(byte* keyStart, byte* keyEnd, byte* valueStart, byte* valueEnd)
    {
        // 跳过 ':' 之后的前导空格和制表符。
        // HTTP 规范（RFC 7230）允许 Header 值前有任意数量的 SP 和 HTAB。
        //
        // Skip leading spaces and tabs after ':'.
        // HTTP spec (RFC 7230) permits any number of SP and HTAB before the header value.
        while (valueStart < valueEnd && (*valueStart == ' ' || *valueStart == '\t'))
            valueStart++;

        // 构造键值零拷贝切片（均直接指向原始读缓冲区，不产生任何内存分配）。
        // Construct zero-copy key and value slices (both point directly into the raw read buffer;
        // no memory allocation occurs).
        UnManagedCollection<byte> keySlice = new UnManagedCollection<byte>(keyStart, (uint)(keyEnd - keyStart));
        UnManagedCollection<byte> valueSlice = new UnManagedCollection<byte>(valueStart, (uint)(valueEnd - valueStart));

        // 快速路径：对五个最常见的 Header 做大小写不敏感的字节序列比较。
        // 命中后直接赋值到对应字段并立即返回，完全绕过哈希表插入操作，显著降低每次请求的开销。
        // 仅未命中的 Header 才落入通用 Headers 字典。
        //
        // Fast path: case-insensitive byte-sequence comparison for the five most common headers.
        // On a hit, assign directly to the corresponding field and return immediately,
        // completely bypassing the hash-table insert and significantly reducing per-request overhead.
        // Only headers not matched here fall through to the general Headers dictionary.

        if (ValueTypeHelper.SequenceEqualIgnoreCase(keySlice.InternalPointer, s_Host.InternalPointer, (int)s_Host.Size))
        {
            HostHeader = valueSlice;
            return;
        }

        if (ValueTypeHelper.SequenceEqualIgnoreCase(keySlice.InternalPointer, s_Connection.InternalPointer, (int)s_Connection.Size))
        {
            ConnectionHeader = valueSlice;
            return;
        }

        if (ValueTypeHelper.SequenceEqualIgnoreCase(keySlice.InternalPointer, s_ContentLength.InternalPointer, (int)s_ContentLength.Size))
        {
            ContentLengthHeader = valueSlice;
            return;
        }

        if (ValueTypeHelper.SequenceEqualIgnoreCase(keySlice.InternalPointer, s_UserAgent.InternalPointer, (int)s_UserAgent.Size))
        {
            UserAgentHeader = valueSlice;
            return;
        }

        if (ValueTypeHelper.SequenceEqualIgnoreCase(keySlice.InternalPointer, s_Cookie.InternalPointer, (int)s_Cookie.Size))
        {
            // Cookie Header 的原始值（如 "session=abc; token=xyz"）暂存到 CookieHeader 字段，
            // 不进入通用 Headers 字典；ProcessHeaders 末尾会统一将其按 ';' 和 '=' 切分为键值对。
            //
            // The raw Cookie header value (e.g. "session=abc; token=xyz") is stored in the
            // CookieHeader field rather than the general Headers dictionary;
            // ProcessHeaders will split it into key-value pairs at the end of the header scan.
            CookieHeader = valueSlice;
            return;
        }

        // 通用路径：未被快速路径命中的 Header 插入 Headers 字典。
        // General path: headers not caught by the fast path are inserted into the Headers dictionary.
        Headers.Add(in keySlice, in valueSlice);
    }

    /// <summary>
    /// 通用键值对切分器：将字节序列按外层分隔符（outerSymbol）切成多个片段，
    /// 再对每个片段按内层分隔符（innerSymbol）切出键和值，存入目标字典。
    /// 所有切片均为零拷贝，直接引用 source 所指向的原始缓冲区，不产生任何内存分配。
    /// <br/>
    /// 使用场景：
    /// <list type="bullet">
    ///   <item><description>QueryString：outerSymbol='&amp;'，innerSymbol='='</description></item>
    ///   <item><description>Form body ：outerSymbol='&amp;'，innerSymbol='='</description></item>
    ///   <item><description>Cookie    ：outerSymbol=';'，innerSymbol='='</description></item>
    /// </list>
    /// <para>
    /// Generic key-value splitter: splits a byte sequence into segments by an outer delimiter
    /// (outerSymbol), then splits each segment at an inner delimiter (innerSymbol) to produce
    /// a key and value that are inserted into the target dictionary.
    /// All slices are zero-copy references to the raw buffer pointed to by source;
    /// no memory allocation occurs.
    /// Use cases:
    /// <list type="bullet">
    ///   <item><description>QueryString: outerSymbol='&amp;', innerSymbol='='</description></item>
    ///   <item><description>Form body:   outerSymbol='&amp;', innerSymbol='='</description></item>
    ///   <item><description>Cookie:      outerSymbol=';',     innerSymbol='='</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="source">待切分的字节序列指针。<para>Pointer to the byte sequence to split.</para></param>
    /// <param name="outerSymbol">片段间的外层分隔符（如 '&amp;' 或 ';'）。<para>Outer delimiter between segments (e.g. '&amp;' or ';').</para></param>
    /// <param name="innerSymbol">键值间的内层分隔符（如 '='）。<para>Inner delimiter between key and value (e.g. '=').</para></param>
    /// <param name="result">用于存储解析结果的目标字典指针。<para>Pointer to the target dictionary for parsed results.</para></param>
    private void SplitByteMapToValueDictionary(
        UnManagedCollection<byte>* source,
        byte outerSymbol,
        byte innerSymbol,
        ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>* result)
    {
        if (source is null || source->IsEmpty) return;

        // 线性扫描字节序列，遇到 outerSymbol 时将 [start, i) 区间切片交给 ProcessPair 处理，
        // 然后将 start 推进到 i+1 继续下一个片段。
        // 扫描结束后，[start, Size) 区间为最后一个（或唯一一个）片段。
        //
        // Linearly scan the byte sequence; when outerSymbol is encountered, pass the slice
        // [start, i) to ProcessPair, then advance start to i+1 for the next segment.
        // After the scan, the range [start, Size) is the last (or only) segment.
        uint start = 0;
        byte* ptr = source->InternalPointer;

        for (uint i = 0; i < source->Size; i++)
        {
            if (*(ptr + i) == outerSymbol)
            {
                ProcessPair(source->Slice(start, i - start), innerSymbol, result);
                start = i + 1;
            }
        }

        // 处理最后一个或唯一一个片段（末尾无 outerSymbol 时必须单独处理）。
        // Process the last or only segment (required when no trailing outerSymbol is present).
        ProcessPair(source->Slice(start), innerSymbol, result);
    }

    /// <summary>
    /// 对单个键值片段做内层分隔符切分，构造零拷贝键值切片后插入字典。
    /// <br/>
    /// 处理规则：
    /// <list type="bullet">
    ///   <item><description>含 innerSymbol（如 "key=value"）：以第一个 innerSymbol 为界切出键和值。</description></item>
    ///   <item><description>不含 innerSymbol（如 "key_only"）：整个片段作为键，值为空。</description></item>
    ///   <item><description>空键（如 "&amp;&amp;" 或 "&amp;=" 产生的空片段）：直接跳过，不插入字典。</description></item>
    /// </list>
    /// <para>
    /// Splits a single key-value segment at the inner delimiter, constructs zero-copy key and
    /// value slices, and inserts them into the dictionary.
    /// Rules:
    /// <list type="bullet">
    ///   <item><description>Contains innerSymbol (e.g. "key=value"): slice at the first innerSymbol.</description></item>
    ///   <item><description>No innerSymbol (e.g. "key_only"): entire segment is the key, value is empty.</description></item>
    ///   <item><description>Empty key (from "&amp;&amp;" or "&amp;="): silently skipped, not inserted.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="pair">待切分的单个键值片段。<para>Single key-value segment to split.</para></param>
    /// <param name="innerSymbol">键值间的分隔符（通常为 '='）。<para>Delimiter between key and value (typically '=').</para></param>
    /// <param name="result">用于存储结果的目标字典指针。<para>Pointer to the target dictionary for the result.</para></param>
    static void ProcessPair(
        UnManagedCollection<byte> pair,
        byte innerSymbol,
        ValueDictionary<UnManagedCollection<byte>, UnManagedCollection<byte>>* result)
    {
        if (pair.IsEmpty) return;

        int equalIndex = pair.IndexOf(innerSymbol);
        UnManagedCollection<byte> key, value;

        if (equalIndex >= 0)
        {
            // "key=value" 形式：以第一个 '=' 为界切出键和值（均为零拷贝切片）。
            // "key=value" form: slice key and value at the first '=' (both are zero-copy slices).
            key = pair.Slice(0, (uint)equalIndex);
            value = pair.Slice((uint)equalIndex + 1);
        }
        else
        {
            // "key" 形式（无 '='）：整段作为键，值为空切片。
            // "key" form (no '='): entire segment is the key, value is an empty slice.
            key = pair;
            value = UnManagedCollection<byte>.Empty;
        }

        // 空键无意义，直接跳过（防止 "&&" 或 "&=" 产生的空键进入字典）。
        // Skip empty keys to prevent them (produced by "&&" or "&=") from entering the dictionary.
        if (!key.IsEmpty)
        {
            result->Add(key, value);
        }
    }
}