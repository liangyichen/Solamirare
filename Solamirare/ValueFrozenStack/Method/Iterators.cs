using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{


    /// <summary>
    /// 获取迭代器
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(_buffer, _count);
    }

    /// <summary>
    /// 迭代器
    /// </summary>
    public ref struct Enumerator
    {
        private readonly T* _buffer;
        private readonly uint _count;
        private uint _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(T* buffer, uint count)
        {
            _buffer = buffer;
            _count = count;
            _index = uint.MaxValue;
        }

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><see langword="true"/> if a next element exists; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            uint index = unchecked(_index + 1);
            if (index < _count)
            {
                _index = index;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a pointer to the current element.
        /// </summary>
        public T* Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => &_buffer[_index];
        }
    }

}
