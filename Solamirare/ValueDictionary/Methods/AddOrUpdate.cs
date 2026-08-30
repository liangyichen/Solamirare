

namespace Solamirare
{

    public unsafe partial struct ValueDictionary<TKey, TValue>
where TKey : unmanaged
where TValue : unmanaged
    {

        /// <summary>
        /// 添加或更新键值对
        /// <para>Adds or updates a key-value pair.</para>
        /// </summary>
        /// <param name="key">键 <para>The key</para></param>
        /// <param name="value">值 <para>The value</para></param>
        /// <returns>如果操作成功返回 true <para>True if the operation was successful</para></returns>
        public bool AddOrUpdate(in TKey key, in TValue value)
        {
            fixed (TValue* pValue = &value)
            fixed (TKey* pKey = &key)
            {
                return AddOrUpdate(pKey, pValue);
            }
        }

        /// <summary>
        /// 添加或更新键值对（指针版本）
        /// <para>Adds or updates a key-value pair (pointer version).</para>
        /// </summary>
        /// <param name="key">键指针 <para>Pointer to the key</para></param>
        /// <param name="value">值指针 <para>Pointer to the value</para></param>
        /// <returns>如果操作成功返回 true <para>True if the operation was successful</para></returns>
        public bool AddOrUpdate(TKey* key, TValue* value)
        {
            if (_disposed || key is null || value is null) return false;

            if (_ctrl == null || _capacity == 0) return false;


            int hashCode = GetKeyHashCode(key);


            int index = FindSlotIndexInternal(key, hashCode);

            if (index != -1)
            {
                _slots[index].Value = *value;
                return true;
            }

            return InsertNew(key, value, hashCode) != -1;
        }

        /// <summary>
        /// 添加或更新键值对（带预计算哈希码）
        /// <para>Adds or updates a key-value pair (with pre-calculated hash code).</para>
        /// </summary>
        /// <param name="key">键指针 <para>Pointer to the key</para></param>
        /// <param name="value">值指针 <para>Pointer to the value</para></param>
        /// <param name="hashCode">预计算的哈希码 <para>Pre-calculated hash code</para></param>
        /// <returns>如果操作成功返回 true <para>True if the operation was successful</para></returns>
        public bool AddOrUpdate(TKey* key, TValue* value, int hashCode)
        {
            if (_disposed || key is null || value is null) return false;
            if (_ctrl == null || _capacity == 0) return false;


            int index = FindSlotIndexInternal(key, hashCode);

            if (index != -1)
            {
                _slots[index].Value = *value;
                return true;
            }

            return InsertNew(key, value, hashCode) != -1;
        }


    }
}
