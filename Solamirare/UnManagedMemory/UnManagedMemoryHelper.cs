namespace Solamirare;

/// <summary>
/// 创建非托管内存
/// <para>Creates unmanaged memory.</para>
/// </summary>
public unsafe static class UnManagedMemoryHelper
{

    /// <summary>
    /// 从 int 转换到字符串，（memory的长度会被自动纠正）
    /// <para>Converts from int to string (the length of memory will be automatically corrected).</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cacheSize">int的位数不可能超过128，128是栈内存可以接受的<para>The number of digits in an int cannot exceed 128, 128 is acceptable for stack memory.</para></param>
    /// <returns></returns>
    public static UnManagedString ParseFromInt(int value, int cacheSize = 128)
    {

        UnManagedString obj = new UnManagedString((uint)cacheSize, 0);

        Span<char> span = obj.AsRealSizeSpan(); //必须利用到所有长度，才最大可能写入成功

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }

    /// <summary>
    /// 从 int 转换到字符串，（memory的容量于长度都会被自动纠正为实际使用量）
    /// <para>Converts from int to string (both capacity and length of memory will be automatically corrected to actual usage).</para>
    /// <para>外部内存的最小长度不能小于16， 否则返回空对象</para>
    /// <para>The minimum length of external memory cannot be less than 16, otherwise an empty object is returned.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="externalMemory">外部内存用于实际存储<para>External memory used for actual storage.</para></param>
    /// <param name="memorySize">外部内存的长度， 不要小于 int 字面的长度， 最小长度不能小于16， 否则返回空对象<para>The length of external memory, do not be less than the literal length of int, minimum length cannot be less than 16, otherwise return an empty object.</para></param>
    /// <returns></returns>
    public static UnManagedString ParseFromInt(int value, char* externalMemory, uint memorySize)
    {
        if (externalMemory is null || memorySize < 16) return new UnManagedString();

        UnManagedString mem = new UnManagedString(externalMemory, memorySize, 0);

        Span<char> span = mem.AsRealSizeSpan(); //必须利用到所有长度，才最大可能写入成功

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            mem.capacity = (uint)charsWritten;
            mem.ReLength((uint)charsWritten);
        }

        return mem;
    }

    /// <summary>
    /// 从 int 转换到字符串，（memory的长度会被自动纠正）
    /// <para>Converts from int to string (the length of memory will be automatically corrected).</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromInt(int value)
    {
        return ParseFromInt(value, 128);
    }

    /// <summary>
    /// 从 decimal 转换到字符串，（memory的长度会被自动纠正）
    /// <para>Converts from decimal to string (the length of memory will be automatically corrected).</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cacheSize">decimal的长度不可能超过256， 默认256是栈内存可以接受的<para>The length of decimal cannot exceed 256, default 256 is acceptable for stack memory.</para></param>
    /// <returns></returns>
    public static UnManagedString ParseFromDecimal(decimal value, int cacheSize = 256)
    {
        UnManagedString obj = new UnManagedString((uint)cacheSize, 0);

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            span = span.Slice(0, charsWritten);

            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }




    /// <summary>
    /// 从 DateTime 转换到字符串，（memory的长度会被自动纠正）
    /// <para>Converts from DateTime to string (the length of memory will be automatically corrected).</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="memory"></param>
    /// <returns></returns>
    public static bool ParseFromDateTime(DateTime value, UnManagedString* memory)
    {
        return ParseFromDateTime(value, memory, "yyyy-MM-ddTHH:mm:ss.fffffff");
    }

    /// <summary>
    /// 从 DateTime 转换到字符串，（memory的长度会被自动纠正）
    /// <para>Converts from DateTime to string (the length of memory will be automatically corrected).</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="memory"></param>
    /// <param name="formatPattern"></param>
    /// <returns></returns>
    static bool ParseFromDateTime(DateTime value, UnManagedString* memory, ReadOnlySpan<char> formatPattern)
    {
        if (memory is null || !memory->Activated) return false;


        Span<char> span = memory->AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten, formatPattern);

        if (success)
        {
            span = span.Slice(0, charsWritten);
            memory->ReLength((uint)charsWritten);
        }

        return success;
    }

    /// <summary>
    /// 从 DateTime 转换到字符串
    /// <para>Converts from DateTime to string.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromDateTime(DateTime value)
    {
        UnManagedString obj = new UnManagedString(128, 0);

        ParseFromDateTime(value, &obj);

        return obj;
    }

    /// <summary>
    /// 从 DateTime 转换到字符串
    /// <para>Converts from DateTime to string.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatPattern">DateTime的长度不可能超过128，128是栈内存可以接受的<para>The length of DateTime cannot exceed 128, 128 is acceptable for stack memory.</para></param>
    /// <returns></returns>
    public static UnManagedString ParseFromDateTime(DateTime value, ReadOnlySpan<char> formatPattern)
    {
        UnManagedString obj = new UnManagedString(128, 0); //DateTime的长度不可能超过128

        ParseFromDateTime(value, &obj, formatPattern);

        return obj;
    }



    /// <summary>
    /// 从 long 转换到字符串，（memory的长度会被自动纠正）
    /// <para>Converts from long to string (the length of memory will be automatically corrected).</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromLong(long value)
    {
        UnManagedString obj = new UnManagedString(256, 0); //long的长度不可能超过256

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            span = span.Slice(0, charsWritten);
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromByte(byte value)
    {
        UnManagedString obj = new UnManagedString(3, 0); // byte max length is 3

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            span = span.Slice(0, charsWritten);

            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="memory"></param>
    /// <returns></returns>
    public static bool ParseFromSbyte(sbyte value, UnManagedString* memory)
    {
        if (memory is null || !memory->Activated) return false;

        Span<char> span = memory->AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            span = span.Slice(0, charsWritten);
            memory->ReLength((uint)charsWritten);
        }

        return success;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromSbyte(sbyte value)
    {
        UnManagedString obj = new UnManagedString(4, 0); // sbyte max length is 4

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            span = span.Slice(0, charsWritten);
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromShort(short value)
    {
        UnManagedString obj = new UnManagedString(6, 0); // short max length is 6

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            span = span.Slice(0, charsWritten);
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="externalMemory"></param>
    /// <param name="externalMemorySize"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromUshort(ushort value, char* externalMemory, uint externalMemorySize)
    {
        if (externalMemory is null) return UnManagedString.Empty;


        UnManagedString obj = new UnManagedString(externalMemory, externalMemorySize, 0);

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromUshort(ushort value)
    {
        UnManagedString obj = new UnManagedString(5, 0); // ushort max length is 5

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            span = span.Slice(0, charsWritten);
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="externalMemory"></param>
    /// <param name="externalMemorySize"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromUint(uint value, char* externalMemory, uint externalMemorySize)
    {
        if (externalMemory is null) return UnManagedString.Empty;

        UnManagedString obj = new UnManagedString(externalMemory, externalMemorySize, 0);

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromUint(uint value)
    {
        UnManagedString obj = new UnManagedString(10, 0); // uint max length is 10

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromUlong(ulong value)
    {
        UnManagedString obj = new UnManagedString(20, 0); // ulong max length is 20

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromFloat(float value)
    {
        UnManagedString obj = new UnManagedString(32, 0); // float max length is 32

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromDouble(double value)
    {
        UnManagedString obj = new UnManagedString(32, 0); // double max length is 32

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromBool(bool value)
    {
        UnManagedString obj = new UnManagedString(5, 0); // bool max length is 5 ("False")

        Span<char> span = obj.AsRealSizeSpan();

        bool success = value.TryFormat(span, out int charsWritten);

        if (success)
        {
            span = span.Slice(0, charsWritten);
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromEnum<TEnum>(TEnum value)
    where TEnum : unmanaged, Enum
    {
        UnManagedString obj = new UnManagedString(128, 0); // Assuming max length of enum string representation is 128

        Span<char> span = obj.AsRealSizeSpan();
        bool success = Enum.TryFormat(value, span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <param name="externalMemory"></param>
    /// <param name="externalMemorySize"></param>
    /// <returns></returns>
    public static UnManagedString ParseFromEnum<TEnum>(TEnum value, char* externalMemory, uint externalMemorySize)
    where TEnum : unmanaged, Enum
    {
        if (externalMemory is null) return UnManagedString.Empty;

        UnManagedString obj = new UnManagedString(externalMemory, externalMemorySize, 0);

        Span<char> span = obj.AsRealSizeSpan();
        bool success = Enum.TryFormat(value, span, out int charsWritten);

        if (success)
        {
            obj.ReLength((uint)charsWritten);
        }

        return obj;
    }





}