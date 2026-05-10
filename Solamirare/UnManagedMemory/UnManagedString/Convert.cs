using System;
using System.Collections.Generic;
using System.Text;

namespace Solamirare;

public static unsafe partial class UnManagedStringHelper
{



    /// <summary>
    /// 将 <see cref="int"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString IntToUnmanagedString(this int value)
    {
        if (value == 0) return "0".CopyToChars();

        UnManagedString str = UnManagedMemoryHelper.ParseFromInt(value, 128);

        return str;
    }


    /// <summary>
    /// 将 <see cref="int"/> 转换为非托管字符串，使用外部缓冲区
    /// <para>外部缓冲区的最小长度不能小于 16，否则返回空对象</para>
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <param name="buffer">用于存储结果的外部内存指针</param>
    /// <param name="bufferSize">外部内存的长度，必须至少为 16</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString IntToUnmanagedString(this int value, char* buffer, uint bufferSize)
    {
        if (value == 0) return "0".CopyToUnManagedMemory(buffer, 1);

        UnManagedString str = UnManagedMemoryHelper.ParseFromInt(value, buffer, bufferSize);

        return str;
    }


    /// <summary>
    /// 将 <see cref="decimal"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString DecimalToUnmanagedString(this decimal value)
    {

        UnManagedString str = UnManagedMemoryHelper.ParseFromDecimal(value, 256);

        return str;
    }

    /// <summary>
    /// 将 <see cref="DateTime"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的时间值</param>
    /// <returns>表示该时间的非托管字符串</returns>
    public static UnManagedString DateTimeToUnmanagedString(this DateTime value)
    {
        UnManagedString str = new UnManagedString(128, 0);

        UnManagedMemoryHelper.ParseFromDateTime(value, &str);

        return str;
    }


    /// <summary>
    /// 将 <see cref="long"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString LongToUnmanagedString(this long value)
    {

        return UnManagedMemoryHelper.ParseFromLong(value);
    }


    /// <summary>
    /// 将 <see cref="byte"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString ByteToUnmanagedString(this byte value)
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromByte(value);
        return str;
    }

    /// <summary>
    /// 将 <see cref="sbyte"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString SbyteToUnmanagedString(this sbyte value)
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromSbyte(value);
        return str;
    }

    /// <summary>
    /// 将 <see cref="short"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString ShortToUnmanagedString(this short value)
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromShort(value);
        return str;
    }

    /// <summary>
    /// 将 <see cref="ushort"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString UshortToUnmanagedString(this ushort value)
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromUshort(value);
        return str;
    }


    /// <summary>
    /// 将 <see cref="uint"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public static UnManagedString UintToUnmanagedString(this uint value)
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromUint(value);
        return str;
    }

    /// <summary>
    /// 将 <see cref="ulong"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public unsafe static UnManagedString UlongToUnmanagedString(this ulong value)
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromUlong(value);
        return str;
    }

    /// <summary>
    /// 将 <see cref="float"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public unsafe static UnManagedString FloatToUnmanagedString(this float value)
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromFloat(value);
        return str;
    }

    /// <summary>
    /// 将 <see cref="double"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public unsafe static UnManagedString DoubleToUnmanagedString(this double value)
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromDouble(value);
        return str;
    }




    /// <summary>
    /// 将 <see cref="bool"/> 转换为非托管字符串
    /// </summary>
    /// <param name="value">要转换的数值</param>
    /// <returns>表示该数值的非托管字符串</returns>
    public unsafe static UnManagedString BoolToUnmanagedString(this bool value)
    {
        UnManagedString str = value ? "true".CopyToChars() : "false".CopyToChars();

        return str;
    }

    /// <summary>
    /// 将枚举类型转换为非托管字符串
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="value">要转换的枚举值</param>
    /// <returns>表示该枚举值的非托管字符串</returns>
    public unsafe static UnManagedString EnumToUnmanagedString<T>(this T value)
    where T : unmanaged, Enum
    {
        UnManagedString str = UnManagedMemoryHelper.ParseFromEnum(value);



        return str;
    }



}
