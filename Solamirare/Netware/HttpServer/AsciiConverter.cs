using System.Runtime.CompilerServices;

/// <summary>
/// 提供了使用指针（unsafe context）将基本数值类型转换为 ASCII 字节序列的方法。
/// </summary>
public static unsafe class AsciiConverter
{
    // 浮点数默认打印的小数位数，用于简化的 Double/Float 转换。
    // 注意：此固定精度转换会损失 double/float 的全部精度。
    private const int DoublePrecisionDigits = 6;

    #region 核心工具方法
    private static int WriteUnsignedAscii(ulong value, byte* buffer)
    {
        byte* start = buffer;
        byte* ptr = buffer;
        int length = 0;

        if (value == 0)
        {
            *ptr++ = (byte)'0';
            return 1;
        }

        while (value > 0)
        {
            *ptr++ = (byte)('0' + (value % 10));
            value /= 10;
            length++;
        }

        byte* left = start;
        byte* right = start + length - 1;
        while (left < right)
        {
            byte swap = *left;
            *left = *right;
            *right = swap;
            left++;
            right--;
        }
        return length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteSignedAscii(long value, byte* buffer)
    {
        int length = 0;

        if (value < 0)
        {
            *buffer++ = (byte)'-';
            length = 1;

            if (value == long.MinValue)
            {
                ulong absValue = (ulong)(-(value + 1)) + 1;
                length += WriteUnsignedAscii(absValue, buffer);
            }
            else
            {
                length += WriteUnsignedAscii((ulong)-value, buffer);
            }
        }
        else
        {
            length = WriteUnsignedAscii((ulong)value, buffer);
        }

        return length;
    }

    private static int WriteString(ReadOnlySpan<byte> s, byte* buffer)
    {
        for (int i = 0; i < s.Length; i++)
        {
            buffer[i] = s[i];
        }

        return s.Length;
    }


    #endregion

    #region 整型转换方法


    /// <summary>Writes an <see cref="int"/> value as ASCII bytes.</summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IntToAscii(int value, byte* buffer) => WriteSignedAscii(value, buffer);

    /// <summary>Writes a <see cref="uint"/> value as ASCII bytes.</summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    public static int UIntToAscii(uint value, byte* buffer) => WriteUnsignedAscii(value, buffer);
    /// <summary>Writes a <see cref="short"/> value as ASCII bytes.</summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    public static int ShortToAscii(short value, byte* buffer) => WriteSignedAscii(value, buffer);
    /// <summary>Writes a <see cref="ushort"/> value as ASCII bytes.</summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    public static int UShortToAscii(ushort value, byte* buffer) => WriteUnsignedAscii(value, buffer);
    /// <summary>Writes a <see cref="long"/> value as ASCII bytes.</summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    public static int LongToAscii(long value, byte* buffer) => WriteSignedAscii(value, buffer);
    /// <summary>Writes a <see cref="ulong"/> value as ASCII bytes.</summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    public static int ULongToAscii(ulong value, byte* buffer) => WriteUnsignedAscii(value, buffer);
    /// <summary>Writes a <see cref="byte"/> value as ASCII bytes.</summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    public static int ByteToAscii(byte value, byte* buffer) => WriteUnsignedAscii(value, buffer);
    /// <summary>Writes an <see cref="sbyte"/> value as ASCII bytes.</summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    public static int SByteToAscii(sbyte value, byte* buffer) => WriteSignedAscii(value, buffer);
    #endregion

    #region Decimal转换方法 (保留完整精度)

    private static void DecimalDivRem10(ref int low, ref int mid, ref int high, out int remainder)
    {
        long current = high;
        high = (int)(current / 10);

        current = (current % 10) * 0x100000000L + (uint)mid;
        mid = (int)(current / 10);

        current = (current % 10) * 0x100000000L + (uint)low;
        low = (int)(current / 10);

        remainder = (int)(current % 10);
    }

    /// <summary>
    /// Writes a <see cref="decimal"/> value as ASCII bytes while preserving decimal scale.
    /// </summary>
    /// <param name="value">Value to write.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <returns>The number of bytes written.</returns>
    public static int DecimalToAscii(decimal value, byte* buffer)
    {
        int[] bits = decimal.GetBits(value);

        int flags = bits[3];
        int scale = (flags >> 16) & 0xFF;
        bool isNegative = (flags & 0x80000000) != 0;

        int low = bits[0];
        int mid = bits[1];
        int high = bits[2];

        byte* currentPtr = buffer;
        int length = 0;

        if (isNegative)
        {
            *currentPtr++ = (byte)'-';
            length++;
        }

        byte* tempStart = currentPtr;
        byte* tempPtr = tempStart;
        int digitsCount = 0;
        int remainder;

        // 持续进行除 10 操作，提取数字
        do
        {
            DecimalDivRem10(ref low, ref mid, ref high, out remainder);
            *tempPtr++ = (byte)('0' + remainder);
            digitsCount++;
        } while (high != 0 || mid != 0 || low != 0 || digitsCount <= scale);

        // 5. 从临时缓冲区反转并写入最终结果，同时插入小数点

        byte* sourcePtr = tempStart + digitsCount - 1;
        int integerDigits = digitsCount - scale;

        if (integerDigits <= 0)
        {
            *currentPtr++ = (byte)'0';
            length++;
            integerDigits = 0;
        }

        // 写入整数部分
        for (int i = 0; i < integerDigits; i++)
        {
            *currentPtr++ = *sourcePtr--;
            length++;
        }

        // 写入小数点和小数部分
        if (scale > 0)
        {
            *currentPtr++ = (byte)'.';
            length++;

            for (int i = 0; i < scale; i++)
            {
                *currentPtr++ = *sourcePtr--;
                length++;
            }
        }

        return length;
    }

    #endregion

    #region 浮点和Double转换方法 (改进但仍有局限性)

    /// <summary>
    /// 将 double 转换为 ASCII 字节序列（简化实现，固定精度，会损失精度）。
    /// **注意：此实现旨在匹配整数转换的思路，但无法实现 IEEE-754 的精确最短表示。**
    /// <para>如果将来有必要进行完整的转换，需要改为调用 UnManagedMemoryHelper 中的对应转换函数</para>
    /// </summary>
    public static int DoubleToAscii(double value, byte* buffer)
    {
        // 检查特殊值
        if (double.IsNaN(value)) return WriteString("NaN"u8, buffer);
        if (double.IsPositiveInfinity(value)) return WriteString("Infinity"u8, buffer);
        if (double.IsNegativeInfinity(value)) return WriteString("-Infinity"u8, buffer);

        byte* currentPtr = buffer;
        int length = 0;

        // 1. 符号
        if (value < 0)
        {
            *currentPtr++ = (byte)'-';
            length++;
            value = -value;
        }

        long wholePart = (long)value;

        // 2. 整数部分
        length += WriteUnsignedAscii((ulong)wholePart, currentPtr);
        // 更新 currentPtr 到整数部分的末尾
        currentPtr += length - (value < 0 ? 1 : 0);

        double fractionalPart = value - wholePart;

        // 3. 小数部分 (如果存在)
        if (fractionalPart > double.Epsilon) // 使用 Epsilon 检查非零
        {
            // 写入小数点
            *currentPtr++ = (byte)'.';
            length++;

            // 循环写入固定位数的小数
            for (int i = 0; i < DoublePrecisionDigits; i++)
            {
                // 确保 fractionalPart 始终保持在 [0, 1) 范围内，并提取下一位数字
                fractionalPart *= 10;
                int digit = (int)fractionalPart;
                *currentPtr++ = (byte)('0' + digit);
                length++;
                // 减去已提取的整数部分，继续下一个小数位
                fractionalPart -= digit;
            }
        }

        return length;
    }

    /// <summary>
    /// 将 float 转换为 ASCII 字节序列（调用 DoubleToAscii）。
    /// </summary>
    public static int FloatToAscii(float value, byte* buffer)
    {
        // float 精度低于 double，转换为 double 后再调用，使用相同的固定精度逻辑。
        return DoubleToAscii(value, buffer);
    }

    #endregion

}
