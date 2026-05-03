// --- File: Transformance.cs ---
namespace Solamirare;

public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{
    /// <summary>
    /// 将当前 UnManagedCollection 转换为另一个类型的 UnManagedCollection，
    /// 实现通过计算新尺寸并重用原始内存块的零GC转换。
    /// 
    /// 注意：该方法在物理内存块大小与目标类型大小不一致时，依赖指针重解释，
    /// 仅适用于原始数据是按字节流意义上被接受为T2布局的场景。
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    /// <returns>一个视图，该视图指向原始内存，但类型为 T2。</returns>
    public UnManagedCollection<T2> Transformance<T2>()
    where T2 : unmanaged
    {
        // 1. 基础检查
        if (InternalPointer is null || size == 0)
        {
            return UnManagedCollection<T2>.Empty;
        }

        // 2. 计算原始数据块的总字节数 (sourceTotalBytes)
        // 这是原始内存块的物理尺寸，是不能改变的。
        long sourceTotalBytes = (long)size * sizeof(T);

        // 3. 目标类型大小检查
        int sizeOfT2 = sizeof(T2);
        if (sizeOfT2 == 0)
        {
            // 目标类型无法确定大小，无法计算 new_size。
            return UnManagedCollection<T2>.Empty;
        }
        
        // 4. 计算新的大小 (new_size)
        // 采用安全的整除操作：计算原始总字节数能容纳的最大 T2 元素数量。
        // 使用 long 确保计算过程不会溢出。
        // 新的长度绝对不会超过原始长度，整数除法的行为就是自动执行“向下取整”（Floor Operation）
        long newSizeLong = sourceTotalBytes / sizeOfT2;

        // 5. 最终检查：如果计算出的尺寸为零，则无法转换。
        if (newSizeLong == 0)
        {
            return UnManagedCollection<T2>.Empty;
        }

        uint newSize = (uint)newSizeLong;

        // 6. 返回结果：创建新的集合视图
        // 我们将 InternalPointer 的内存块，以新的 size，作为 T2 类型的视图来暴露。
        // 这满足了“不分配新内存”和“类型转换”的要求。
        return new UnManagedCollection<T2>((T2*)InternalPointer, newSize);
    }
}
