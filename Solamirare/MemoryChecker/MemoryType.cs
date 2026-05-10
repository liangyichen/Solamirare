namespace Solamirare;

/// <summary>
/// 标识当前指针指向的内存是属于堆分配或栈分配
/// </summary>
public enum MemoryType : byte
{
    /// <summary>
    /// The memory is on the current thread's stack.
    /// </summary>
    Stack = 0,

    /*
    /// <summary>
    /// 目前 Heap 和 Unknown 不能准确区分，不能以该值作为逻辑判断依据
    /// </summary>
    Heap = 1,

    /// <summary>
    /// The memory is part of a loaded module (e.g., code in a DLL/EXE).
    /// </summary>
    ModuleImage = 2,
    */

    /// <summary>
    /// 未分配的内存，指向 null
    /// </summary>
    Unallocated = 3,

    /// <summary>
    /// 有可能是堆内存，也有可能是空内存，或指向文件 dll、exe 等的内存，或方法指针， 总之不会是栈内存
    /// </summary>
    Unknown = 4
}

