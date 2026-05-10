using System;
using System.Collections.Generic;
using System.Text;

namespace Solamirare;



public unsafe partial struct UHttpResponse
{


   
    /// <summary>
    /// 写入原始字节数据
    /// </summary>
    /// <param name="content">要写入的字节 Span</param>
    public void Write(ReadOnlySpan<byte> content)
    {
        if (content.IsEmpty) return;
        byte* dest = GetSafeWritePtr(content.Length);
        if (dest != null)
        {
            // 优化 2：用 NativeMemory.Copy 直接指针拷贝，与 WriteBytes 保持一致，
            // 避免构造 Span 对象的额外开销。
            fixed (byte* src = content)
                NativeMemory.Copy(src, dest, (nuint)content.Length);
            _bodyLength += content.Length;
        }
    }

    /// <summary>
    /// 写入字符串内容
    /// </summary>
    /// <param name="content">要写入的字符 Span</param>
    public void Write(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty) return;
        // 先用 GetByteCount 计算精确字节数，再做边界检查。
        // GetByteCount 是纯计算，无堆分配，额外开销极小。
        int exactBytes = Encoding.UTF8.GetByteCount(content);
        byte* dest = GetSafeWritePtr(exactBytes);
        if (dest != null)
            _bodyLength += Encoding.UTF8.GetBytes(content, new Span<byte>(dest, exactBytes));
    }

    /// <summary>
    /// 写入字符串内容
    /// </summary>
    /// <param name="p">字符指针</param>
    /// <param name="size">字符数量</param>
    public void Write(char* p, uint size)
    {
        if (p == null || size == 0) return;
        
        int exactBytes = Encoding.UTF8.GetByteCount(p, (int)size);
        byte* dest = GetSafeWritePtr(exactBytes);
        if (dest != null)
            _bodyLength += Encoding.UTF8.GetBytes(p, (int)size, dest, exactBytes);
    }

    /// <summary>
    /// 写入 short 值
    /// </summary>
    public void Write(short value) { byte* d = GetSafeWritePtr(8); if (d != null) _bodyLength += AsciiConverter.ShortToAscii(value, d); } // 8: short 字符串最大长度

    /// <summary>
    /// 写入 ushort 值
    /// </summary>
    public void Write(ushort value) { byte* d = GetSafeWritePtr(8); if (d != null) _bodyLength += AsciiConverter.UShortToAscii(value, d); } // 8: ushort 字符串最大长度

    /// <summary>
    /// 写入 int 值
    /// </summary>
    public void Write(int value) { byte* d = GetSafeWritePtr(12); if (d != null) _bodyLength += AsciiConverter.IntToAscii(value, d); } // 12: int 字符串最大长度

    /// <summary>
    /// 写入 uint 值
    /// </summary>
    public void Write(uint value) { byte* d = GetSafeWritePtr(12); if (d != null) _bodyLength += AsciiConverter.UIntToAscii(value, d); } // 12: uint 字符串最大长度

    /// <summary>
    /// 写入 long 值
    /// </summary>
    public void Write(long value) { byte* d = GetSafeWritePtr(24); if (d != null) _bodyLength += AsciiConverter.LongToAscii(value, d); } // 24: long 字符串最大长度

    /// <summary>
    /// 写入 ulong 值
    /// </summary>
    public void Write(ulong value) { byte* d = GetSafeWritePtr(24); if (d != null) _bodyLength += AsciiConverter.ULongToAscii(value, d); } // 24: ulong 字符串最大长度

    /// <summary>
    /// 写入 float 值
    /// </summary>
    public void Write(float value) { byte* d = GetSafeWritePtr(32); if (d != null) _bodyLength += AsciiConverter.FloatToAscii(value, d); } // 32: float 字符串最大长度

    /// <summary>
    /// 写入 double 值
    /// </summary>
    public void Write(double value) { byte* d = GetSafeWritePtr(32); if (d != null) _bodyLength += AsciiConverter.DoubleToAscii(value, d); } // 32: double 字符串最大长度

    /// <summary>
    /// 写入 bool 值
    /// </summary>
    public void Write(bool value) => Write(value ? "true" : "false");

    /// <summary>
    /// 写入非托管内存中的字符串内容
    /// </summary>
    public void Write(in UnManagedString content) => Write(content.Pointer, content.UsageSize);


    /// <summary>
    /// 写入非托管内存中的字符串内容
    /// </summary>
    public void Write(UnManagedString* content) { if (content != null) Write(content->Pointer, content->UsageSize); }


    /// <summary>
    /// 写入非托管内存中的字节序列内容
    /// </summary>
    public void Write(in UnManagedMemory<byte> content) => WriteBytes(content.Pointer, (int)content.UsageSize);


    /// <summary>
    /// 写入非托管内存中的字节序列内容
    /// </summary>
    public void Write(UnManagedMemory<byte>* content) { if (content != null) WriteBytes(content->Pointer, (int)content->UsageSize); }


    /// <summary>
    /// 写入非托管集合中的字节序列内容
    /// </summary>
    public void Write(UnManagedCollection<byte>* content) { if (content != null) WriteBytes(content->InternalPointer, (int)content->Size); }


    /// <summary>
    /// 写入非托管集合中的字节序列内容
    /// </summary>
    public void Write(in UnManagedCollection<byte> content) { if (!content.IsEmpty) WriteBytes(content.InternalPointer, (int)content.Size); }


    /// <summary>
    /// 写入非托管集合中的字符串内容
    /// </summary>
    public void Write(UnManagedCollection<char>* content) { if (content != null) Write(content->InternalPointer, content->Size); }

    /// <summary>
    /// 写入非托管集合中的字符串内容
    /// </summary>
    public void Write(in UnManagedCollection<char> content) { if (!content.IsEmpty) Write(content.InternalPointer, content.Size); }

    /// <summary>
    /// 写入枚举值的字符串表示
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">枚举值</param>
    /// <param name="charsLength">临时缓冲区长度</param>
    public void Write<TEnum>(TEnum value, uint charsLength = 128) where TEnum : unmanaged, Enum // 128: 默认枚举字符串缓冲区长度
    {
        // 内存安全：
        // 512 字符（1024 字节）足以覆盖任何合理的枚举字符串，超出部分直接截断。
        if (charsLength > 512) charsLength = 512; // 512: 枚举字符串栈缓冲区上界
        char* temp = stackalloc char[(int)charsLength];
        UnManagedString mem = UnManagedMemoryHelper.ParseFromEnum(value, temp, charsLength);
        Write(mem);
        mem.Dispose();
    }

    /// <summary>
    /// 写入原始字节数据
    /// </summary>
    /// <param name="data">字节数据指针</param>
    /// <param name="length">数据长度</param>
    public void WriteBytes(byte* data, int length)
    {
        if (data is null || length <= 0) return;
        byte* dest = GetSafeWritePtr(length);
        if (dest != null)
        {
            NativeMemory.Copy(data, dest, (nuint)length);
            _bodyLength += length;
        }
    }

  

}









