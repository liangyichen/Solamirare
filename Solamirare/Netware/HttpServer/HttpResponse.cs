using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// HttpResponse
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct UHttpResponse
{
    // 响应状态常量
    private const byte STATE_NONE = 0;
    private const byte STATE_SERIALIZED = 1;
    private const byte STATE_FINALIZED = 2;
    private const byte STATE_DISPOSED = 3;

    // 预编码的常量字符串 (UTF-8)
    private static ReadOnlySpan<byte> Http11 => "HTTP/1.1 "u8;
    private static ReadOnlySpan<byte> StatusPlaceholder => "200 OK          \r\n"u8;
    private static ReadOnlySpan<byte> ConnKeepAlive => "Connection: keep-alive\r\n"u8;
    private static ReadOnlySpan<byte> ConnClose => "Connection: close\r\n"u8;
    private static ReadOnlySpan<byte> ContentTypeHeader => "Content-Type: "u8;
    private static ReadOnlySpan<byte> ContentLengthHeader => "Content-Length: "u8;



    /// <summary>
    /// Server Header 的值切片，默认为 "Solamirare"。
    /// 用户可在首次调用 Write 之前赋值以覆盖默认值。
    /// 在 EnsureHeaderSerialized 中直接写入缓冲区，不经过 Headers 字典。
    /// </summary>
    public UnManagedCollection<byte> ServerHeader;



    // Date / Server Header 名称的静态字节序列，供 EnsureHeaderSerialized 直接写入缓冲区。
    private static ReadOnlySpan<byte> DateHeaderKey => "Date: "u8;
    private static ReadOnlySpan<byte> ServerHeaderKey => "Server: "u8;
    // Server Header 默认值。
    // Default value for the Server header.
    private static ReadOnlySpan<byte> DefaultServerName => "Solamirare"u8;

    /// <summary>
    /// HTTP 响应头集合（仅存放用户自定义的动态条目）。
    /// Date / Server / Content-Type / Connection / Content-Length 均由硬编码路径直接写入，不经过此字典。
    /// </summary>
    private ValueDictionary<UnManagedMemory<byte>, UnManagedMemory<byte>> Headers;




    // GMT 时间字符串的栈内存（29 字节实际使用，32 字节对齐到缓存行）。
    // Date Header 直接从此内存写入缓冲区，不再装入 Headers 字典。
    fixed byte GMTStringMemory[32];


    // 在 UHttpResponse 类里新增两个静态字段
    // 上次生成的 GMT 字符串（29 字节）
    static readonly byte[] s_cachedGMT = new byte[29];

    // 上次生成时的 Ticks（Environment.TickCount64，毫秒级，无系统调用）
    static long s_cachedGMTTicks = 0;

    /// <summary>
    /// 指向底层响应缓冲区的指针
    /// </summary>
    public byte* ResponseBuffer;

    /// <summary>
    /// 关联的 HTTP 上下文
    /// </summary>
    internal UHttpContext* context;

    MemoryPoolCluster* memoryPool;

    /// <summary>
    /// 指向响应体在缓冲区中起始位置的指针
    /// </summary>
    private byte* _bodyStartPtr;

    /// <summary>
    /// 指向 Content-Length 头部值预留位置的指针，用于后续回填
    /// </summary>
    private byte* _clValuePtr;

    /// <summary>
    /// 指向状态码起始位置的指针
    /// </summary>
    private byte* _statusDigitPtr;

    /// <summary>
    /// 指向缓冲区结束位置的指针，用于边界检查
    /// </summary>
    private byte* _bufferEnd;


    /// <summary>
    /// 当前已写入的响应体长度
    /// </summary>
    private int _bodyLength;

    /// <summary>
    /// 响应内容的 MIME 类型
    /// </summary>
    public HttpMimeTypes ResponseContentType;

    /// <summary>
    /// 当前响应对象的生命周期状态
    /// </summary>
    private byte _state;

    /// <summary>
    /// 是否抑制头部序列化（例如用于 WebSocket 握手或直接透传）
    /// </summary>
    internal bool SuppressHeaderSerialization;



    void reset()
    {
        ResponseBuffer = null;
        _bufferEnd = null;
        _bodyLength = 0;
        _bodyStartPtr = null;
        _clValuePtr = null;
        _statusDigitPtr = null;
        ResponseContentType = HttpMimeTypes.TextPlain;
        _state = STATE_NONE;
        SuppressHeaderSerialization = false;


        // 优化 5：重置 ServerHeader 为默认值（零拷贝切片，指向静态字面量）。
        // Opt 5: reset ServerHeader to its default value (zero-copy slice into the static literal).
        ServerHeader = DefaultServerName;

        context = null;
    }


    /// <summary>
    /// HttpResponse
    /// </summary>
    /// <param name="config"></param>
    /// <param name="context"></param>
    /// <param name="responseBuffer"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Init(HTTPSeverConfig* config, UHttpContext* context, byte* responseBuffer)
    {
        // 内存安全：已销毁的实例禁止重新初始化；reset() 会将 _state 写回 STATE_NONE，
        if (_state == STATE_DISPOSED) return;

        if (responseBuffer == null || context == null)
            return;


        reset();

        this.context = context;

        ResponseBuffer = responseBuffer;

        _bufferEnd = responseBuffer + config->RESPONSE_BUFFER_CAPACITY;



        // 初始化 GMT 时间字符串，写入栈内存 GMTStringMemory，供 EnsureHeaderSerialized 直接使用。
        fixed (byte* p_GMTStringMemory = GMTStringMemory)
        {
            long now = Environment.TickCount64; // 纯内存读，无系统调用
            if (now - s_cachedGMTTicks >= 1000) // 超过 1 秒才重新生成
            {
                UnManagedMemory<byte> gmt_mem = new UnManagedMemory<byte>(
                    p_GMTStringMemory, 29, 29, MemoryTypeDefined.Stack);
                DateTime.UtcNow.GMTBytes(&gmt_mem); // 每秒最多调用一次
                fixed (byte* pCache = s_cachedGMT)
                    NativeMemory.Copy(p_GMTStringMemory, pCache, 29);
                s_cachedGMTTicks = now;
            }
            else
            {
                // 直接从缓存复制，29 字节，单条 memcpy 指令
                fixed (byte* pCache = s_cachedGMT)
                    NativeMemory.Copy(pCache, p_GMTStringMemory, 29);
            }
        }
    }


    /// <summary>
    /// 添加一个响应头；如果键已存在则更新其值。
    /// </summary>
    /// <param name="key">响应头名称。</param>
    /// <param name="value">响应头值。</param>
    public void AddOrUpdateHeader(UnManagedMemory<byte> key, UnManagedMemory<byte> value)
    {
        if (!Headers.Created)
        {
            Headers = new ValueDictionary<UnManagedMemory<byte>, UnManagedMemory<byte>>(4);
        }
        Headers.AddOrUpdate(key, value);
    }

    private void EnsureHeaderSerialized()
    {
        if (_state >= STATE_SERIALIZED || _state == STATE_DISPOSED) return;

        if (SuppressHeaderSerialization)
        {
            _bodyStartPtr = ResponseBuffer;
            _state = STATE_SERIALIZED;
            return;
        }

        byte* cur = ResponseBuffer;

        // 内存安全：缓冲区不足时不能静默 return（_state 仍为 STATE_NONE），否则后续每次
        // Write 调用都会重入此方法并再次静默失败，调用方完全无法感知错误。
        // 正确做法：释放 Headers 字典并将状态置为 STATE_DISPOSED，使所有后续操作快速失败。
        
        // 512: 头部序列化所需的最小安全空间 / minimum safe space for header serialization
        if (cur + 512 > _bufferEnd)
        {
            if (Headers.Created)
            {
                Headers.ForEach(&UHttpContext.DictionaryDisposeWithInnerLoop, null);
                Headers.Dispose();
            }
            _state = STATE_DISPOSED;
            return;
        }

        // 1. 协议起始行
        // 对固定小块数据直接走指针拷贝，避免每次构造 Span 对象的额外开销。全文同此规则。
        fixed (byte* pHttp11 = Http11)
            Unsafe.CopyBlockUnaligned(cur, pHttp11, (uint)Http11.Length);
        cur += Http11.Length;

        _statusDigitPtr = cur;
        fixed (byte* pStatus = StatusPlaceholder)
            Unsafe.CopyBlockUnaligned(cur, pStatus, (uint)StatusPlaceholder.Length);
        cur += StatusPlaceholder.Length;

        // 2. 序列化 Headers

        if (Headers.Created && Headers.Count > 0)
        {
            // 在栈上分配 2 个指针的空间，避免数组初始化器错误
            void** args = stackalloc void*[2]; // 2: 参数数量

            args[0] = &cur;

            args[1] = _bufferEnd;

            Headers.ForEach(&serializeHeaderToBuffer, args);
        }

        // 3. Connection (动态处理 Keep-Alive)
        bool keepAlive = true;
        if (context != null)
        {
            UnManagedCollection<byte> connValue = context->Request.ConnectionHeader;
            if (!connValue.IsEmpty && connValue.Equals("close"u8))
            {
                keepAlive = false;
            }
        }

        if (keepAlive)
        {
            fixed (byte* p = ConnKeepAlive)
                Unsafe.CopyBlockUnaligned(cur, p, (uint)ConnKeepAlive.Length);
            cur += ConnKeepAlive.Length;
        }
        else
        {
            fixed (byte* p = ConnClose)
                Unsafe.CopyBlockUnaligned(cur, p, (uint)ConnClose.Length);
            cur += ConnClose.Length;
        }

        // Content-Type
        if (ResponseContentType != HttpMimeTypes.Unknown)
        {
            UnManagedMemory<byte>* mimeType = ServerVariables.Mimetypes[ResponseContentType];
            if (mimeType != null && !mimeType->IsEmpty)
            {
                fixed (byte* pCT = ContentTypeHeader)
                    Unsafe.CopyBlockUnaligned(cur, pCT, (uint)ContentTypeHeader.Length);
                cur += ContentTypeHeader.Length;
                NativeMemory.Copy(mimeType->Pointer, cur, mimeType->UsageSize);
                cur += mimeType->UsageSize;
                *cur++ = 13; // 13: CR (\r)
                *cur++ = 10; // 10: LF (\n)
            }
        }

        // 4. Content-Length 预留区
        fixed (byte* pCL = ContentLengthHeader)
            Unsafe.CopyBlockUnaligned(cur, pCL, (uint)ContentLengthHeader.Length);
        cur += ContentLengthHeader.Length;

        _clValuePtr = cur;

        // 用 Unsafe.InitBlock 单指令填充 13 字节空格，替代原来的逐字节循环。
        // 预留 13 字节足够容纳 int 最大值（10 位）；回填时被数字覆盖，剩余字节保持空格。
        Unsafe.InitBlock(cur, 32, 13); // 32: ASCII space / ASCII 空格
        cur += 13;
        *cur++ = 13; *cur++ = 10; // \r\n — Content-Length 行结束 / end of Content-Length line

        // 5. Date 直写：从 GMTStringMemory 直接拷贝到缓冲区，不经过 Headers 字典。
        fixed (byte* pDateKey = DateHeaderKey)
            Unsafe.CopyBlockUnaligned(cur, pDateKey, (uint)DateHeaderKey.Length);
        cur += DateHeaderKey.Length;
        fixed (byte* pGMT = GMTStringMemory)
            Unsafe.CopyBlockUnaligned(cur, pGMT, 29); // 29: GMT 字符串固定长度 / fixed GMT string length
        cur += 29;
        *cur++ = 13; *cur++ = 10; // \r\n

        // 5. Server 直写：从 ServerHeader 字段直接拷贝，用户可在首次 Write 前覆盖此字段。
        // 内存安全：ServerHeader 可能被用户赋为空集合，InternalPointer 为 null，
        // 必须先检查 IsEmpty，否则 NativeMemory.Copy 会触发访问违例。
        if (!ServerHeader.IsEmpty)
        {
            fixed (byte* pServerKey = ServerHeaderKey)
                Unsafe.CopyBlockUnaligned(cur, pServerKey, (uint)ServerHeaderKey.Length);
            cur += ServerHeaderKey.Length;
            fixed (UnManagedCollection<byte>* pSrv = &ServerHeader)
            {
                NativeMemory.Copy(pSrv->InternalPointer, cur, pSrv->Size);
                cur += pSrv->Size;
            }
            *cur++ = 13; *cur++ = 10; // \r\n
        }

        // 头部结束：两个连续 CRLF
        // End of headers: two consecutive CRLFs
        *cur++ = 13; *cur++ = 10;
        *cur++ = 13; *cur++ = 10;

        _bodyStartPtr = cur;
        Headers.Dispose();
        _state = STATE_SERIALIZED;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte* GetSafeWritePtr(int requestedSize)
    {
        if (_state == STATE_NONE) EnsureHeaderSerialized();
        if (_state >= STATE_FINALIZED || _bodyStartPtr == null) return null;

        byte* targetPtr = _bodyStartPtr + _bodyLength;
        
        if (targetPtr + requestedSize > _bufferEnd) return null;
        return targetPtr;
    }


    /// <summary>
    /// 完成响应，回填状态码和 Content-Length
    /// </summary>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <returns>是否成功</returns>
    /// <summary>
    /// 将指定状态码对应的描述文本（Reason Phrase）写入预留占位区。
    /// 占位符 "200 OK          \r\n" 共 18 字节，格式为
    /// [3位状态码][空格][最多12字节短语][补空格至固定宽度][\r\n]。
    /// </summary>
    private static ReadOnlySpan<byte> GetReasonPhrase(int statusCode) => statusCode switch
    {
        200 => "OK"u8,
        201 => "Created"u8,
        204 => "No Content"u8,
        301 => "Moved Permanently"u8,
        302 => "Found"u8,
        304 => "Not Modified"u8,
        400 => "Bad Request"u8,
        401 => "Unauthorized"u8,
        403 => "Forbidden"u8,
        404 => "Not Found"u8,
        405 => "Method Not Allowed"u8,
        408 => "Request Timeout"u8,
        409 => "Conflict"u8,
        410 => "Gone"u8,
        413 => "Content Too Large"u8,
        414 => "URI Too Long"u8,
        415 => "Unsupported Media Type"u8,
        422 => "Unprocessable Content"u8,
        429 => "Too Many Requests"u8,
        500 => "Internal Server Error"u8,
        501 => "Not Implemented"u8,
        502 => "Bad Gateway"u8,
        503 => "Service Unavailable"u8,
        504 => "Gateway Timeout"u8,
        _ => "Unknown"u8,
    };

    /// <summary>
    /// 完成响应：回填状态码、Reason Phrase 和 Content-Length。
    /// </summary>
    /// <param name="statusCode">HTTP 状态码，默认 200。</param>
    /// <returns>成功返回 true；状态不合法返回 false。</returns>
    public bool FinalizeResponse(int statusCode = 200)
    {
        if (_state != STATE_SERIALIZED) return false;

        // SuppressHeaderSerialization 分支必须设置 STATE_FINALIZED 和 TotalResponseLength，
        // 否则同一 Response 可被无限次调用，且连接层拿不到正确的发送长度。
        if (SuppressHeaderSerialization)
        {
            if (context != null && _bodyStartPtr != null)
                context->TotalResponseLength = (uint)(_bodyStartPtr - ResponseBuffer) + (uint)_bodyLength;

            _state = STATE_FINALIZED;
            return true;
        }

        // 将 statusCode 写入 _statusDigitPtr 指向的占位区。
        // 占位符格式（共 18 字节）："200 OK          

        //   偏移 0~2  : 3 位状态码数字
        //   偏移 3    : 空格
        //   偏移 4~N  : Reason Phrase（写完后剩余位置补空格，保持固定宽度）
        //   最后 2 字节: 
        //（由 StatusPlaceholder 预置，不覆盖）
        
        if (_statusDigitPtr != null)
        {
            // 常量提前声明，供边界检查和后续写入共同使用。
            const int PhraseOffset = 4;  // 状态码(3) + 空格(1)
            const int PhraseMaxLen = 12; // StatusPlaceholder 中短语区宽度

            // 内存安全：写入范围为 _statusDigitPtr[0..PhraseOffset+PhraseMaxLen-1]（共 16 字节），
            // 必须确认完全在缓冲区内，防止越界写入。
            if (_statusDigitPtr + PhraseOffset + PhraseMaxLen > _bufferEnd) return false;

            // 写入 3 位状态码（直接字节操作，避免通用格式化开销）
            int s = statusCode;
            _statusDigitPtr[0] = (byte)('0' + s / 100);
            _statusDigitPtr[1] = (byte)('0' + s / 10 % 10);
            _statusDigitPtr[2] = (byte)('0' + s % 10);


            // 偏移 3 已由 StatusPlaceholder 写入空格，不需重写。
            // 写入 Reason Phrase，覆盖占位符中的 "OK          " 区域（最多 12 字节 + 补空格）
            ReadOnlySpan<byte> phrase = GetReasonPhrase(statusCode);
            int writeLen = phrase.Length < PhraseMaxLen ? phrase.Length : PhraseMaxLen;

            // 优化 4：用 NativeMemory.Copy 写入短语字节，用 Unsafe.InitBlock 补剩余空格，
            // 消除两个逐字节 for 循环的控制开销。
            fixed (byte* pPhrase = phrase)
                NativeMemory.Copy(pPhrase, _statusDigitPtr + PhraseOffset, (nuint)writeLen);
            if (writeLen < PhraseMaxLen)
                Unsafe.InitBlock(_statusDigitPtr + PhraseOffset + writeLen, (byte)' ', (uint)(PhraseMaxLen - writeLen));
        }

        // 回填 Content-Length 数值（从 _clValuePtr + 2 写入，前 2 字节保留为空格）
        if (_clValuePtr != null && _clValuePtr < _bufferEnd)
            // 直接从预留区起始写入。
            AsciiConverter.IntToAscii(_bodyLength, _clValuePtr);

        if (context != null && _bodyStartPtr != null)
            context->TotalResponseLength = (uint)(_bodyStartPtr - ResponseBuffer) + (uint)_bodyLength;

        _state = STATE_FINALIZED;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool serializeHeaderToBuffer(int index, UnManagedMemory<byte>* key, UnManagedMemory<byte>* value, void* caller)
    {
        // 内存安全：caller 为 null 是真正的调用错误，终止迭代（return false）是合理的。
        // 但 key/value 为 null 或 key 为空只是数据问题：此时 value 可能持有非托管内存，
        // 若直接 return false 会中断 ForEach 迭代，导致后续所有条目的内存无法释放（泄漏）。
        // 正确做法：对数据问题执行 Dispose 后 return true，让迭代继续。
        
        if (caller is null) return false;
        if (key is null || value is null || key->IsEmpty)
        {
            if (key != null) key->Dispose();
            if (value != null) value->Dispose();
            return true;
        }

        void** args = (void**)caller;
        byte** cur = (byte**)args[0];
        byte* end = (byte*)args[1];

        uint needed = key->UsageSize + value->UsageSize + 4; // 4: ": \r\n"

        // 内存安全：原来用 >= 会把最后一个合法字节也拒掉（差一字节错误），改为 >。
        if (*cur + needed > end)
        {
            // BUG 3 修复：缓冲区不足时不能直接 return false，否则 ForEach 会中断迭代，
            // 导致后续所有 Header 的 key/value 持有的非托管内存无法被释放，造成内存泄漏。
            // 正确做法是释放当前 Header 的资源后 return true，让迭代继续处理剩余 Header。
            key->Dispose();
            value->Dispose();
            return true;
        }

        // 用 NativeMemory.Copy 直接指针拷贝替代 AsSpan().CopyTo(new Span<byte>(...))，
        // 避免 Span 对象构造和托管拷贝分派的额外开销，与 WriteBytes 的实现保持一致。

        // Key
        NativeMemory.Copy(key->Pointer, *cur, key->UsageSize);
        *cur += key->UsageSize;
        // ": "
        **cur = (byte)':'; *(*cur + 1) = (byte)' '; *cur += 2; // 2: ": "
        // Value
        NativeMemory.Copy(value->Pointer, *cur, value->UsageSize);
        *cur += value->UsageSize;
        // "\r\n"
        **cur = 13; *(*cur + 1) = 10; *cur += 2; // 13: CR, 10: LF, 2: "\r\n"

        key->Dispose();
        value->Dispose();
        return true;
    }


    /// <summary>
    /// 描述当前对象状态的哈希码。
    /// </summary>
    public ulong StatusCode
    {
        get
        {
            fixed(UHttpResponse* p = &this)
            {
                ulong result = Fingerprint.MemoryFingerprint(p);

                return result;
            }
        }
    }

    /// <summary>
    /// 清空响应内容与状态，以便复用当前响应对象。
    /// </summary>
    public void Clear()
    {
        // 内存安全：首次 Init 之前 Headers.Created 为 false，直接调用 Clear() 会访问
        // 未初始化的内部指针，导致未定义行为；必须先检查 Created。
        if (Headers.Created)
        {
            Headers.ForEach(&UHttpContext.DictionaryDisposeWithInnerLoop, null);
            Headers.Clear();
        }

        reset();
    }




    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_state == STATE_DISPOSED) return;

        // 将隐式状态组合改为显式条件判断。
        // 只有当 Headers 字典已创建（Created == true）且状态仍为 STATE_NONE 时，
        // 说明 EnsureHeaderSerialized 从未执行完毕（例如因缓冲区不足提前 return），
        // 此时 Headers 内的 key/value 尚未通过 serializeHeaderToBuffer 逐一 Dispose，
        // 需要在此处补充释放。
        // 若 _state >= STATE_SERIALIZED，则 EnsureHeaderSerialized 已在末尾调用过
        // Headers.Dispose()，此处不应重复释放。
        // SuppressHeaderSerialization == true 时 EnsureHeaderSerialized 不序列化 Headers，
        // 同样需要在此释放。
        if (Headers.Created && (_state == STATE_NONE || SuppressHeaderSerialization))
        {
            Headers.ForEach(&UHttpContext.DictionaryDisposeWithInnerLoop, null);
            Headers.Dispose();
        }

        _state = STATE_DISPOSED;
    }
}
