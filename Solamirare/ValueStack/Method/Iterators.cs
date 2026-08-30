using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    /// <summary>
    /// 获取迭代器
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        fixed (ValueStack<T>* p = &this)
        {
            return new Enumerator(p);
        }
    }

    /// <summary>
    /// 迭代器
    /// </summary>
    public ref struct Enumerator
    {
        private readonly StackSegment<T>* _segments;
        private readonly uint _segmentCount;
        private readonly ulong _count;

        private ulong _index;
        private uint _currentSegmentIndex;
        private StackSegment<T>* _currentSegmentPtr;
        private ulong _currentLocalIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ValueStack<T>* stack)
        {
            _segments = stack->_segments;
            _segmentCount = stack->_segmentCount;
            _count = stack->_count;
            _index = ulong.MaxValue;

            _currentSegmentIndex = 0;
            _currentSegmentPtr = _segments;
            _currentLocalIndex = 0;
        }

        /// <summary>
        /// Advances the enumerator to the next stack element.
        /// </summary>
        /// <returns><see langword="true"/> if a next element exists; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ulong nextIndex = unchecked(_index + 1);
            if (nextIndex >= _count) return false;

            _index = nextIndex;

            if (_index == 0)
            {
                _currentLocalIndex = 0;
                _currentSegmentPtr = _segments;
                return true;
            }

            _currentLocalIndex++;

            if (_currentLocalIndex >= _currentSegmentPtr->Capacity)
            {
                _currentSegmentIndex++;
                _currentSegmentPtr++;
                _currentLocalIndex = 0;
            }

            return true;
        }

        /// <summary>
        /// Gets a pointer to the current stack element.
        /// </summary>
        public T* Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentSegmentPtr->DataPtr + _currentLocalIndex;
        }
    }

}
