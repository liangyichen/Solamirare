// /Users/liangyichen/Documents/github_site/Solamirare_Dev/Solamirare/ValueDictionary/UnmanagedDictionaryEnumerator.cs

using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// 通用的非托管字典迭代器，支持 Swiss Table 和 Double Hashing 布局
/// </summary>
/// <summary>
/// Enumerates occupied slots from unmanaged dictionary storage.
/// </summary>
public unsafe ref struct DictionaryEnumerator<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    private readonly DictionarySlot<TKey, TValue>* _slots;
    private readonly byte* _states;
    private readonly uint _capacity;
    private readonly int* _versionPtr;
    private int _version;
    private int _index;

    internal DictionaryEnumerator(DictionarySlot<TKey, TValue>* slots, byte* states, uint capacity, int* versionPtr)
    {
        _slots = slots;
        _states = states;
        _capacity = capacity;
        _versionPtr = versionPtr;
        _version = versionPtr != null ? *versionPtr : 0;
        _index = -1;
    }

    /// <summary>
    /// Advances the enumerator to the next occupied slot.
    /// </summary>
    /// <returns><see langword="true"/> if a next slot exists; otherwise <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        if (_versionPtr != null && *_versionPtr != _version)
            throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

        // 统一遍历逻辑：只要状态字节 <= 0x7F 即视为有效元素
        // 兼容 Swiss Table (H2 Hash) 和 DictionaryEntryState.Occupied (0x01)
        while (++_index < _capacity)
        {
            if (_states[_index] <= 0x7F)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 同步版本号（用于支持 RemoveCurrent 等操作）
    /// </summary>
    public void SyncVersion()
    {
        if (_versionPtr != null) _version = *_versionPtr;
    }

    /// <summary>
    /// Gets a pointer to the current slot.
    /// </summary>
    public readonly DictionarySlot<TKey, TValue>* Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => &_slots[_index];
    }

    /// <summary>
    /// Gets the current slot index.
    /// </summary>
    public readonly int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _index;
    }
}

/// <summary>
/// Represents a key-only collection view over unmanaged dictionary storage.
/// </summary>
public unsafe ref struct UnmanagedKeyCollection<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    private readonly DictionarySlot<TKey, TValue>* _slots;
    private readonly byte* _states;
    private readonly uint _capacity;
    private readonly int* _versionPtr;

    internal UnmanagedKeyCollection(DictionarySlot<TKey, TValue>* slots, byte* states, uint capacity, int* versionPtr)
    {
        _slots = slots; _states = states; _capacity = capacity; _versionPtr = versionPtr;
    }

    /// <summary>
    /// Returns an enumerator over keys.
    /// </summary>
    /// <returns>A key enumerator.</returns>
    public UnmanagedKeyEnumerator<TKey, TValue> GetEnumerator() => new UnmanagedKeyEnumerator<TKey, TValue>(_slots, _states, _capacity, _versionPtr);
}

/// <summary>
/// Represents a value-only collection view over unmanaged dictionary storage.
/// </summary>
public unsafe ref struct UnmanagedValueCollection<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    private readonly DictionarySlot<TKey, TValue>* _slots;
    private readonly byte* _states;
    private readonly uint _capacity;
    private readonly int* _versionPtr;

    internal UnmanagedValueCollection(DictionarySlot<TKey, TValue>* slots, byte* states, uint capacity, int* versionPtr)
    {
        _slots = slots; _states = states; _capacity = capacity; _versionPtr = versionPtr;
    }

    /// <summary>
    /// Returns an enumerator over values.
    /// </summary>
    /// <returns>A value enumerator.</returns>
    public UnmanagedValueEnumerator<TKey, TValue> GetEnumerator() => new UnmanagedValueEnumerator<TKey, TValue>(_slots, _states, _capacity, _versionPtr);
}

/// <summary>
/// Enumerates keys from unmanaged dictionary storage.
/// </summary>
public unsafe ref struct UnmanagedKeyEnumerator<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    private DictionaryEnumerator<TKey, TValue> _inner;
    internal UnmanagedKeyEnumerator(DictionarySlot<TKey, TValue>* slots, byte* states, uint capacity, int* versionPtr)
    {
        _inner = new DictionaryEnumerator<TKey, TValue>(slots, states, capacity, versionPtr);
    }
    /// <summary>
    /// Advances the enumerator to the next key.
    /// </summary>
    /// <returns><see langword="true"/> if a next key exists; otherwise <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => _inner.MoveNext();

    /// <summary>
    /// Gets a pointer to the current key.
    /// </summary>
    public readonly TKey* Current => &_inner.Current->Key;
}

/// <summary>
/// Enumerates values from unmanaged dictionary storage.
/// </summary>
public unsafe ref struct UnmanagedValueEnumerator<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    private DictionaryEnumerator<TKey, TValue> _inner;
    internal UnmanagedValueEnumerator(DictionarySlot<TKey, TValue>* slots, byte* states, uint capacity, int* versionPtr)
    {
        _inner = new DictionaryEnumerator<TKey, TValue>(slots, states, capacity, versionPtr);
    }
    /// <summary>
    /// Advances the enumerator to the next value.
    /// </summary>
    /// <returns><see langword="true"/> if a next value exists; otherwise <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => _inner.MoveNext();

    /// <summary>
    /// Gets a pointer to the current value.
    /// </summary>
    public readonly TValue* Current => &_inner.Current->Value;
}
