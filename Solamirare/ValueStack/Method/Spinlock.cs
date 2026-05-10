using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    /// <summary>
    /// 自旋锁
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AcquireSpinlock()
    {
        // 尝试从 0 (Unlocked) 交换到 1 (Locked)
        while (Interlocked.CompareExchange(ref _spinlock, 1, 0) != 0)
        {
            // 如果未能获取锁，则自旋等待一小段时间 (避免占用核心，让步给其他线程)
            // 在 C# 中，Thread.Yield() 比空循环更高效，因为它会通知调度器
            Thread.Yield();
        }
    }

    /// <summary>
    /// 解除自旋锁
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReleaseSpinlock()
    {
        // 直接将锁状态设置为 0 (Unlocked)
        // 使用 Interlocked.Exchange 确保写入操作的原子性和内存屏障
        Interlocked.Exchange(ref _spinlock, 0);
    }

}