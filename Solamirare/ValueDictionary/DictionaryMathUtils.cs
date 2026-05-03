using System.Runtime.CompilerServices;

namespace Solamirare;


/// <summary>
/// Provides helper methods for dictionary capacity and threshold calculations.
/// </summary>
public static class DictionaryMathUtils
{
    /// <summary>
    /// Gets the minimum supported dictionary capacity.
    /// </summary>
    public const uint MinCapacity = 4;


    /// <summary>
    /// Calculates the resize threshold for a capacity using a 75% load factor.
    /// </summary>
    /// <param name="capacity">Bucket capacity.</param>
    /// <returns>The item count threshold that triggers growth.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CalculateThreshold(uint capacity) => capacity - (capacity >> 2);

    /// <summary>
    /// 将容量向上取整到最近的 2 的幂
    /// </summary>
    public static uint NextPowerOfTwo(uint x)
    {
        if (x < 2) return MinCapacity; // 统一处理 0 和 1 的输入
        if (x == 0) return MinCapacity;
        x--;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }
}
