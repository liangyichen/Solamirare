using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe partial struct ValueDictionary<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{

    /// <summary>
    /// slot 索引查找
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashCode"></param>
    /// <returns></returns>
    int FindSlotIndexInternal(TKey* key, int hashCode = -1)
    {
        int index;

        //目前尚需解决：
        // 1： byte 类型的时候如何比较
        // 2： 鉴于 key 的长度都是很小的， 是否应该改为标量计算会比较好
        // 3： 以下两个查找方法立即就开始 vector128 计算， 缺少x64的高级SIMD加速，也缺少回退到标量的补充

        Type typeof_key = typeof(TKey);

        if (typeof_key == SolamirareEnvironment.Type_UnManagedMemory_Char || typeof_key == SolamirareEnvironment.Type_UnManagedCollection_Char)
        {
            UnManagedCollection<char>* mem = (UnManagedCollection<char>*)key;

            index = InnerEntry(mem, out _);
        }
        else if (typeof_key == typeof(UnManagedMemory<byte>) || typeof_key == typeof(UnManagedCollection<byte>))
        {

            UnManagedCollection<byte>* mem = (UnManagedCollection<byte>*)key;

            index = InnerEntry(mem, out _);
        }
        else
        {
            if (hashCode == -1)

                hashCode = GetKeyHashCode(key);

            uint h1 = GetH1(hashCode);

            byte h2 = GetH2(hashCode);

            return FindEntry(key, h1, h2);

        }

        return index;
    }


    /// <summary>
    /// 统一写入逻辑
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CommitSlot(int index, TKey* key, TValue* value, int hashCode, byte h2)
    {
        if (key is null || value is null) return;

        if (_ctrl[index] == ByteDeleted) _deletedCount--;

        _slots[index].Key = *key;
        _slots[index].Value = *value;
        _slots[index].HashCode = hashCode;

        // 1. 写入主控制区
        _ctrl[index] = h2;

        // 2. 关键：同步镜像区
        _ctrl[_capacity + index] = h2;

        _count++;
    }


}