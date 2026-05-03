using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct ValueDictionary<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{

    /// <summary>
    /// Returns an enumerator that iterates through the occupied dictionary slots.
    /// </summary>
    /// <returns>A slot enumerator for the current dictionary.</returns>
    public Enumerator GetEnumerator()
    {
        fixed (ValueDictionary<TKey, TValue>* p_this = &this) return new Enumerator(p_this);
    }

    /// <summary>
    /// Enumerates occupied slots in a <see cref="ValueDictionary{TKey, TValue}"/>.
    /// </summary>
    public ref struct Enumerator
    {
        private DictionaryEnumerator<TKey, TValue> _inner;
        private readonly ValueDictionary<TKey, TValue>* _dict;

        internal Enumerator(ValueDictionary<TKey, TValue>* dict)
        {
            _dict = dict;
            _inner = new DictionaryEnumerator<TKey, TValue>(dict->_slots, dict->_ctrl, dict->_capacity, &dict->_version);
        }

        /// <summary>
        /// Advances the enumerator to the next occupied slot.
        /// </summary>
        /// <returns><see langword="true"/> if a next slot exists; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _inner.MoveNext();

        /// <summary>
        /// Gets a pointer to the current dictionary slot.
        /// </summary>
        public readonly DictionarySlot<TKey, TValue>* Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _inner.Current;
        }

        /// <summary>
        /// Removes the slot currently referenced by the enumerator.
        /// </summary>
        public void RemoveCurrent()
        {
            int index = _inner.Index;
            // 确保索引有效且当前位置确实被占用 (<= 0x7F)
            if (index >= 0 && index < _dict->_capacity && _dict->_ctrl[index] <= 0x7F)
            {
                _dict->MarkDeleted(index);
                _dict->_count--;
                _dict->_deletedCount++;
                _dict->_version++;
                _inner.SyncVersion();
            }
        }
    }

    /// <summary>
    /// Gets a collection view over the dictionary keys.
    /// </summary>
    public KeyCollection Keys => new KeyCollection(ref this);

    /// <summary>
    /// Represents a key-only collection view for the dictionary.
    /// </summary>
    public readonly ref struct KeyCollection
    {
        private readonly ValueDictionary<TKey, TValue>* _dict;
        internal KeyCollection(ref ValueDictionary<TKey, TValue> dict) { fixed (ValueDictionary<TKey, TValue>* p = &dict) _dict = p; }
        /// <summary>
        /// Returns an enumerator over the dictionary keys.
        /// </summary>
        /// <returns>A key enumerator.</returns>
        public KeyEnumerator GetEnumerator() => new KeyEnumerator(_dict);
    }

    /// <summary>
    /// Enumerates keys in the dictionary.
    /// </summary>
    public ref struct KeyEnumerator
    {
        private DictionaryEnumerator<TKey, TValue> _inner;
        internal KeyEnumerator(ValueDictionary<TKey, TValue>* dict) { _inner = new DictionaryEnumerator<TKey, TValue>(dict->_slots, dict->_ctrl, dict->_capacity, &dict->_version); }
        /// <summary>
        /// Advances the enumerator to the next key.
        /// </summary>
        /// <returns><see langword="true"/> if a next key exists; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _inner.MoveNext();

        /// <summary>
        /// Gets the current key.
        /// </summary>
        public readonly ref TKey Current => ref _inner.Current->Key;
    }

    /// <summary>
    /// Gets a collection view over the dictionary values.
    /// </summary>
    public ValueCollection Values => new ValueCollection(ref this);

    /// <summary>
    /// Represents a value-only collection view for the dictionary.
    /// </summary>
    public readonly ref struct ValueCollection
    {
        private readonly ValueDictionary<TKey, TValue>* _dict;
        internal ValueCollection(ref ValueDictionary<TKey, TValue> dict) { fixed (ValueDictionary<TKey, TValue>* p = &dict) _dict = p; }
        /// <summary>
        /// Returns an enumerator over the dictionary values.
        /// </summary>
        /// <returns>A value enumerator.</returns>
        public ValueEnumerator GetEnumerator() => new ValueEnumerator(_dict);
    }

    /// <summary>
    /// Enumerates values in the dictionary.
    /// </summary>
    public ref struct ValueEnumerator
    {
        private DictionaryEnumerator<TKey, TValue> _inner;
        internal ValueEnumerator(ValueDictionary<TKey, TValue>* dict) { _inner = new DictionaryEnumerator<TKey, TValue>(dict->_slots, dict->_ctrl, dict->_capacity, &dict->_version); }
        /// <summary>
        /// Advances the enumerator to the next value.
        /// </summary>
        /// <returns><see langword="true"/> if a next value exists; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _inner.MoveNext();

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public readonly ref TValue Current => ref _inner.Current->Value;
    }


}
