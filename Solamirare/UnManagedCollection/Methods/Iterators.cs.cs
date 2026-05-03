using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 获取迭代器
    /// <para>Gets the enumerator.</para>
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(InternalPointer, Size);
    }

    /// <summary>
    /// 迭代器
    /// <para>Enumerator.</para>
    /// </summary>
    public ref struct Enumerator
    {
        private readonly T* _pointer;
        private readonly uint _length;
        private uint _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(T* pointer, uint length)
        {
            _pointer = pointer;
            _length = length;
            _index = uint.MaxValue;
        }

        /// <summary>
        /// 将枚举器移动到下一个元素。
        /// </summary>
        /// <returns>若成功移动到下一个元素则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            uint index = unchecked(_index + 1);
            if (index < _length)
            {
                _index = index;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 当前元素
        /// <para>Current element.</para>
        /// </summary>
        public T* Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => &_pointer[_index];
        }
    }

}
