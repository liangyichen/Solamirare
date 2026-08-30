using System.Runtime.CompilerServices;

namespace Solamirare;


/// <summary>
/// 内存对齐计算
/// </summary>
public static class MemoryAlignmentHelper
{
    
    /// <summary>
    /// 缓存行数字对齐
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Align(int value)
    {

        int MASK = (int)SolamirareEnvironment.ALIGNMENT - 1; // 63，二进制 00111111

        // 逻辑：(40 + 63) & ~63
        // 103 & ...111111000000 = 64
        // 注意：~63 在补码中等于 -64
        return (value + MASK) & ~MASK;
    }

    /// <summary>
    /// 指针对齐
    /// </summary>
    /// <param name="ptr"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte* AlignPointer(byte* ptr)
    {
        int MASK = (int)SolamirareEnvironment.ALIGNMENT - 1; // 63，二进制 00111111

        // 指针版本同理
        return (byte*)(((nuint)ptr + (nuint)MASK) & ~(nuint)MASK);
    }
}