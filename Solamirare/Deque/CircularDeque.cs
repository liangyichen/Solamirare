using System.Runtime.CompilerServices;
using System.Security;

namespace Solamirare
{

    /// <summary>
    /// 双端队列，支持在两端高效地添加和移除元素。
    /// </summary>
    /// <typeparam name="T">
    /// 队列中元素的类型。
    /// </typeparam>
    [SecurityCritical]
    [Guid(SolamirareEnvironment.CircularDequeGuid)]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CircularDeque<T> where T : unmanaged
    {

        private const int DEFAULT_CAPACITY = 16;

        private T* _buffer;

        private int _head;      // 逻辑数据的起始索引 (PopFront位置)

        private int _tail;      // 下一个空闲槽位的索引 (PushBack位置)

        private int _capacity;

        private int _count;

        bool frozen;


        /// <summary>
        /// 获取双端队列中包含的元素数。
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// 获取双端队列的容量。
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// 初始化 CircularDeque 类的新实例。
        /// </summary>
        public CircularDeque() : this(DEFAULT_CAPACITY)
        {

        }

        /// <summary>
        /// 初始化 CircularDeque 类的新实例。
        /// </summary>
        /// <param name="initialCapacity">
        /// 初始容量。默认值为 16。
        /// </param>
        /// <param name="frozen">是否锁定容量，如果锁定，则不能进行扩容操作</param>
        public CircularDeque(int initialCapacity = DEFAULT_CAPACITY, bool frozen = false)
        {
            if (initialCapacity <= 0) initialCapacity = DEFAULT_CAPACITY;


            // 强制容量为 2 的幂，以便使用位运算优化索引
            _capacity = (int)DictionaryMathUtils.NextPowerOfTwo((uint)initialCapacity);
            _count = 0;
            _head = 0;
            _tail = 0;
            this.frozen = frozen;


            nuint totalBytes = (nuint)_capacity * (nuint)sizeof(T);

            _buffer = (T*)NativeMemory.AllocZeroed(totalBytes);

        }

        // ----------------------------------------------------
        // 核心索引计算：确保索引在 [0, _capacity - 1] 范围内循环
        // ----------------------------------------------------
        /// <summary>
        /// 核心索引计算：确保索引在 [0, _capacity - 1] 范围内循环。
        /// </summary>
        /// <param name="index">
        /// 原始索引。
        /// </param>
        /// <returns>
        /// 映射后的缓冲区索引。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Index(int index)
        {
            // 使用位运算代替取模 (前提：_capacity 必须是 2 的幂)
            return index & (_capacity - 1);
        }

        // ----------------------------------------------------
        // 核心操作
        // ----------------------------------------------------

        /// <summary>
        /// 将对象添加到双端队列的开头。
        /// </summary>
        /// <param name="item">
        /// 要添加到双端队列开头的对象。
        /// </param>
        public void PushFront(T item)
        {
            bool next = true;

            if (_count == _capacity)
            {
                next = Resize(_capacity == 0 ? DEFAULT_CAPACITY : _capacity * 2);
            }

            if (!next) return;

            // 1. 将 head 向后移动并循环
            _head = Index(_head - 1);

            // 2. 存储数据
            _buffer[_head] = item;

            _count++;
        }

        /// <summary>
        /// 将对象添加到双端队列的末尾。
        /// </summary>
        /// <param name="item">
        /// 要添加到双端队列末尾的对象。
        /// </param>
        public void PushBack(T item)
        {
            bool next = true;

            if (_count == _capacity)
            {
                next = Resize(_capacity == 0 ? DEFAULT_CAPACITY : _capacity * 2);
            }

            if (!next) return;

            // 1. 存储数据在 tail 位置
            _buffer[_tail] = item;

            // 2. 将 tail 向前移动并循环
            _tail = Index(_tail + 1);

            _count++;
        }

        /// <summary>
        /// 移除并返回位于双端队列开头的对象。
        /// </summary>
        /// <returns>
        /// 位于双端队列开头的对象。
        /// </returns>
        public T PopFront()
        {
            if (_count == 0) return default;

            T item = _buffer[_head];

            // 2. 移动 head 并循环
            _head = Index(_head + 1);

            _count--;
            return item;
        }

        /// <summary>
        /// 移除并返回位于双端队列末尾的对象。
        /// </summary>
        /// <returns>
        /// 位于双端队列末尾的对象。
        /// </returns>
        public T PopBack()
        {
            if (_count == 0) return default;

            // 1. 将 tail 向后移动并循环
            _tail = Index(_tail - 1);

            T item = _buffer[_tail];


            _count--;
            return item;
        }

        // ----------------------------------------------------
        // 调整容量
        // ----------------------------------------------------

        /// <summary>
        /// 调整内部缓冲区的大小。
        /// </summary>
        /// <param name="newCapacity">
        /// 新的容量。
        /// </param>
        private bool Resize(int newCapacity)
        {
            if (newCapacity == _capacity || frozen) return false;

            // 1. 分配新的更大的缓冲区
            T* newBuffer;

            nuint newSize = (nuint)newCapacity * (nuint)sizeof(T);

            newBuffer = (T*)NativeMemory.AllocZeroed(newSize);


            if (_count > 0)
            {
                // 环形缓冲区的数据可能被分割成两段
                int startSegmentSize = _capacity - _head;

                if (_tail > _head)
                {
                    // 情况 A: 数据是连续的 (Head...Tail)
                    NativeMemory.Copy(_buffer + _head, newBuffer, (nuint)_count * (nuint)sizeof(T));
                }
                else
                {
                    // 情况 B: 数据是环绕的 (尾部...头部)
                    // 段 1: 从 _head 到缓冲区末尾
                    NativeMemory.Copy(_buffer + _head, newBuffer, (nuint)startSegmentSize * (nuint)sizeof(T));

                    // 段 2: 从缓冲区开始 (0) 到 _tail
                    NativeMemory.Copy(_buffer, newBuffer + startSegmentSize, (nuint)_tail * (nuint)sizeof(T));
                }
            }


            if (_buffer != null)
            {
                NativeMemory.Free(_buffer);
            }

            _buffer = newBuffer;
            _capacity = newCapacity;

            // 在新缓冲区中，数据现在是线性的，Head/Tail 重新对齐
            _head = 0;
            _tail = _count;

            return true;
        }


        /// <summary>
        /// 从双端队列中移除所有对象。
        /// </summary>
        public void Clear()
        {
            // 安全地清空所有活跃数据占据的内存
            if (_count > 0)
            {
                if (_tail > _head)
                {
                    NativeMemory.Clear(_buffer + _head, (nuint)_count * (nuint)sizeof(T));
                }
                else
                {
                    int startSegmentSize = _capacity - _head;
                    NativeMemory.Clear(_buffer + _head, (nuint)startSegmentSize * (nuint)sizeof(T));
                    NativeMemory.Clear(_buffer, (nuint)_tail * (nuint)sizeof(T));
                }
            }

            // 重置索引到空状态
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// 释放由双端队列使用的所有资源。
        /// </summary>
        public void Dispose()
        {
            if (_buffer != null)
            {
                NativeMemory.Free(_buffer);
            }
            _buffer = null;
            _capacity = 0;
            _count = 0;
            _head = 0;
            _tail = 0;
        }



        /// <summary>
        /// 描述当前对象状态的哈希码。
        /// </summary>
        public ulong StatusCode
        {
            get
            {
                fixed (CircularDeque<T>* p = &this)
                {
                    ulong result = Fingerprint.MemoryFingerprint(p);

                    return result;
                }
            }
        }

        /// <summary>
        /// 清除所有未使用的容量。
        /// </summary>
        public void TrimExcess()
        {
            if (_count < _capacity && !frozen)
            {
                int newCapacity = (int)DictionaryMathUtils.NextPowerOfTwo((uint)_count);
                if (newCapacity < _capacity)
                {
                    Resize(newCapacity);
                }
            }
        }
    }

}
