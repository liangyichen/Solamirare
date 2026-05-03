using System.Runtime.CompilerServices;

/// <summary>
/// 随机数生成器
/// </summary>
public ref struct RandomNumberGenerator
{
    private uint _state;


    /// <summary>
    /// 使用当前时间的 Ticks 作为种子初始化
    /// </summary>
    public RandomNumberGenerator()
    {
        _state = (uint)DateTime.UtcNow.Ticks;
    }


    /// <summary>
    /// 使用指定的种子初始化
    /// </summary>
    /// <param name="seed"></param>
    public RandomNumberGenerator(int seed)
    {
        _state = (uint)seed;
    }


    /// <summary>
    /// 使用 Xorshift 算法生成下一个伪随机数
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Next()
    {
        uint x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return x;
    }

    /// <summary>
    /// 在一个范围内生成一个随机数 [min, max)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next(int min, int max)
    {
        // 使用取模操作将随机数映射到指定范围
        // (max - min) 是范围的大小
        return min + (int)(Next() % (uint)(max - min));
    }
}