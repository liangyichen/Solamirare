using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Solamirare;


public unsafe partial struct ValueDictionary<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    /// <summary>
    /// Mixes a raw hash code into the dictionary's probe hash representation.
    /// </summary>
    /// <param name="hashCode">Raw hash code.</param>
    /// <returns>The mixed hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Mix(int hashCode)
    {
        uint h = (uint)hashCode;
        h *= 0xcc9e2d51;
        h = (h << 15) | (h >> 17);
        h *= 0x1b873593;
        return h;
    }


    /// <summary>
    /// Gets the primary probe hash from a raw hash code.
    /// </summary>
    /// <param name="hashCode">Raw hash code.</param>
    /// <returns>The primary probe hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetH1(int hashCode) => Mix(hashCode) >> 7;

    /// <summary>
    /// Gets the secondary 7-bit control-byte hash from a raw hash code.
    /// </summary>
    /// <param name="hashCode">Raw hash code.</param>
    /// <returns>The control-byte hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetH2(int hashCode) => (byte)(Mix(hashCode) & 0x7F);


    /// <summary>
    /// Finds the next free slot index in the control-byte table.
    /// </summary>
    /// <param name="ctrl">Control-byte table pointer.</param>
    /// <param name="capacity">Dictionary capacity.</param>
    /// <param name="hashCode">Hash code used to choose the probe start.</param>
    /// <returns>The free slot index, or <c>-1</c> when the control table is unavailable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindFreeSlot(byte* ctrl, uint capacity, int hashCode)
    {
        if (ctrl == null) return -1;

        uint mask = capacity - 1;
        uint index = GetH1(hashCode) & mask;

        while (true)
        {
            Vector128<byte> group = Vector128.Load(ctrl + index);

            // 优化：利用 MSB (最高位) 快速查找 Empty(0xFF) 或 Deleted(0xFE)
            // Occupied (0x00-0x7F) 的 MSB 为 0，Deleted/Empty 的 MSB 为 1
            uint freeMask = group.ExtractMostSignificantBits();
            if (freeMask != 0)
                return (int)((index + (uint)BitOperations.TrailingZeroCount(freeMask)) & mask);

            index = (index + 16) & mask;
        }
    }




    /// <summary>
    /// Writes a key/value pair into a slot and updates the control-byte table.
    /// </summary>
    /// <param name="ctrl">Control-byte table pointer.</param>
    /// <param name="slots">Slot storage pointer.</param>
    /// <param name="capacity">Dictionary capacity.</param>
    /// <param name="index">Slot index to write.</param>
    /// <param name="key">Key pointer.</param>
    /// <param name="value">Value pointer.</param>
    /// <param name="hashCode">Cached key hash code.</param>
    /// <param name="h2">Secondary control-byte hash.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSlot(
        byte* ctrl,
        DictionarySlot<TKey, TValue>* slots,
        uint capacity,
        int index,
        TKey* key,
        TValue* value,
        int hashCode,
        byte h2)
    {
        if (ctrl == null || slots == null || key == null || value == null) return;

        slots[index].Key = *key;
        slots[index].Value = *value;
        slots[index].HashCode = hashCode;
        ctrl[index] = h2;
        // If the write is within the first 16 bytes, update the mirror at the end.
        if (index < 16)
        {
            ctrl[capacity + index] = h2;
        }
    }



    internal int InnerEntry<T>(UnManagedCollection<T>* key, out int hashCode)
    where T : unmanaged
    {
        if (_disposed || _ctrl == null || key is null)
        {
            hashCode = -1;
            return -1;
        }

        Type typeof_key = typeof(TKey);


        if (typeof_key == typeof(UnManagedMemory<T>) || typeof_key == typeof(UnManagedCollection<T>))
        {
            // 1. 计算 HashCode (必须与 Append 时使用的算法一致)
            // The insertion path (AddOrUpdate -> FindIndexByContent) uses Murmur3 on the span. We must do the same.
            hashCode = (int)ValueTypeHelper.HashCode(key->InternalPointer, (int)key->Size);


            uint h1 = GetH1(hashCode);
            byte h2 = GetH2(hashCode);

            uint mask = _capacity - 1;
            uint index = h1 & mask;

            Vector128<byte> vH2 = Vector128.Create(h2);
            Vector128<byte> vEmpty = Vector128.Create(ByteEmpty);

            while (true)
            {
                Vector128<byte> group = Vector128.Load(_ctrl + index);

                // 匹配 H2
                uint matchMask = Vector128.Equals(group, vH2).ExtractMostSignificantBits();
                // 匹配 Empty
                uint emptyMask = Vector128.Equals(group, vEmpty).ExtractMostSignificantBits();

                if (emptyMask != 0)
                {
                    // 屏蔽 Empty 之后的匹配
                    matchMask &= (1u << BitOperations.TrailingZeroCount(emptyMask)) - 1;
                }

                while (matchMask != 0)
                {
                    int bitIndex = BitOperations.TrailingZeroCount(matchMask);
                    int actualIndex = (int)((index + bitIndex) & mask);

                    UnManagedCollection<T>* slotKey = (UnManagedCollection<T>*)&_slots[actualIndex].Key;

                    bool isMatch = false;
                    if (slotKey->Size == key->Size)
                    {
                        if (typeof(T) == typeof(byte) && key->Size <= 8)
                        {
                            byte* p1 = (byte*)slotKey->InternalPointer;
                            byte* p2 = (byte*)key->InternalPointer;
                            isMatch = true;
                            for (uint i = 0; i < key->Size; i++) if (p1[i] != p2[i]) { isMatch = false; break; }
                        }
                        else if (typeof(T) == typeof(char) && key->Size <= 4)
                        {
                            char* p1 = (char*)slotKey->InternalPointer;
                            char* p2 = (char*)key->InternalPointer;
                            isMatch = true;
                            for (uint i = 0; i < key->Size; i++) if (p1[i] != p2[i]) { isMatch = false; break; }
                        }
                        else
                        {
                            isMatch = slotKey->Equals(key);
                        }
                    }

                    if (isMatch) return actualIndex;

                    matchMask &= ~(1u << bitIndex);
                }

                if (emptyMask != 0) return -1;

                index = (index + 16) & mask;
            }
        }


        hashCode = -1;
        return -1;

    }




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int FindEntry(TKey* key, uint h1, byte h2)
    {
        if (_ctrl == null || _slots == null || key == null) return -1;

        uint mask = _capacity - 1;
        uint index = h1 & mask;

        Vector128<byte> vH2 = Vector128.Create(h2);
        Vector128<byte> vEmpty = Vector128.Create((byte)0xFF);

        while (true)
        {
            Vector128<byte> group = Vector128.Load(_ctrl + index);
            uint matchMask = Vector128.Equals(group, vH2).ExtractMostSignificantBits();
            uint emptyMask = Vector128.Equals(group, vEmpty).ExtractMostSignificantBits();

            if (emptyMask != 0)
            {
                // 优化：屏蔽掉第一个 Empty 之后的所有匹配项
                matchMask &= (1u << BitOperations.TrailingZeroCount(emptyMask)) - 1;
            }

            while (matchMask != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(matchMask);
                uint actualIndex = (index + (uint)bitIndex) & mask;

                bool isMatch = false;
                if (sizeof(TKey) <= 8)
                {
                    byte* p1 = (byte*)&_slots[actualIndex].Key;
                    byte* p2 = (byte*)key;
                    isMatch = true;
                    for (int i = 0; i < sizeof(TKey); i++) if (p1[i] != p2[i]) { isMatch = false; break; }
                }
                else
                {
                    isMatch = ValueTypeHelper.IndexOf(&_slots[actualIndex].Key, 1, key, 1) == 0;
                }

                if (isMatch) return (int)actualIndex;

                matchMask &= ~(1u << bitIndex);
            }

            if (emptyMask != 0)
            {
                return -1;
            }

            index = (index + 16) & mask;
        }
    }




    /// <summary>
    /// Marks a slot as deleted in the control-byte table.
    /// </summary>
    /// <param name="index">Slot index to mark as deleted.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkDeleted(int index)
    {
        if (_ctrl == null) return;

        _ctrl[index] = 0xFE;
        // If the deletion is within the first 16 bytes, update the mirror at the end.
        if (index < 16)
        {
            _ctrl[_capacity + index] = 0xFE;
        }
    }

}
