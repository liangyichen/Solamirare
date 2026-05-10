
namespace Solamirare;


public unsafe partial struct ValueDictionary<TKey, TValue>
where TKey : unmanaged
where TValue : unmanaged
{

    /// <summary>
    /// 更新键值对
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool Update(in TKey key, in TValue value)
    {
        fixed (TValue* pValue = &value)
        fixed (TKey* pKey = &key)
        {
            return Update(pKey, pValue);
        }
    }

    /// <summary>
    /// 更新键值对
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool Update(TKey* key, TValue* value)
    {
        if (_disposed || key is null || value is null) return false;

        if (_ctrl == null || _capacity == 0) return false;

        int hashCode = GetKeyHashCode(key);

        int index = FindSlotIndexInternal(key, hashCode);

        return update(key, value, index);

    }

    /// <summary>
    /// 更新键值对
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="hashCode"></param>
    /// <returns></returns>
    public bool Update(TKey* key, TValue* value, int hashCode)
    {
        if (_disposed || key is null || value is null) return false;

        if (_ctrl == null || _capacity == 0) return false;

        int index = FindSlotIndexInternal(key, hashCode);

        return update(key, value, index);
    }



    bool update(TKey* key, TValue* value, int index)
    {
        if (index != -1)
        {
            _slots[index].Value = *value;
            return true;
        }

        return false;
    }

}
