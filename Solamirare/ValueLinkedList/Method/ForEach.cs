using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 遍历元素
    /// <para>函数指针参数依次是：下标，元素指针，caller回传，是否继续迭代</para>
    /// </summary>
    /// <param name="onload">函数指针参数依次是：下标，元素指针，caller回传，是否继续迭代</param>
    /// <param name="caller">传递到函数指针的第三个参数</param>
    /// <returns></returns>
    public void ForEach(delegate*<int, T*, void*, bool> onload, void* caller)
    {
        ValueLiskedListNode<T>* current = head; // 从头节点开始
        int i = 0;

        while (current != null)
        {
            // 检查 Value 是否为空指针
            if (current->Value != null)
            {
                bool load = onload(i, current->Value, caller);

                if (!load) break;

                i += 1;
            }

            current = current->Next; // 移动到下一个节点
        }
    }

    /// <summary>
    /// 获取迭代器
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        return new Enumerator(head);
    }

    /// <summary>
    /// 迭代器
    /// </summary>
    public ref struct Enumerator
    {
        private ValueLiskedListNode<T>* _current;
        private ValueLiskedListNode<T>* _next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ValueLiskedListNode<T>* head)
        {
            _current = null;
            _next = head;
        }

        /// <summary>
        /// Advances the enumerator to the next linked-list node.
        /// </summary>
        /// <returns><see langword="true"/> if a next node exists; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _current = _next;
            if (_current != null)
            {
                _next = _current->Next;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a pointer to the current node value.
        /// </summary>
        public T* Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current->Value;
        }
    }

}
