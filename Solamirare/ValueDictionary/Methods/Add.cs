namespace Solamirare
{

    public unsafe partial struct ValueDictionary<TKey, TValue>
 where TKey : unmanaged
 where TValue : unmanaged
    {

        /// <summary>
        /// 添加键值对
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        public bool Add(TKey* key, TValue* value, int hashCode)
        {
            if (_disposed || key is null || value is null) return false;
            if (_ctrl == null || _capacity == 0) return false;


            return InsertNew(key, value, hashCode) != -1;
        }


        /// <summary>
        /// 添加键值对
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Add(TKey* key, TValue* value)
        {
            if (_disposed || key is null || value is null) return false;
            if (_ctrl == null || _capacity == 0) return false;

            int hashCode = GetKeyHashCode(key);

            return InsertNew(key, value, hashCode) != -1;
        }

        /// <summary>
        /// 添加键值对
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Add(in TKey key, in TValue value)
        {
            fixed (TValue* pValue = &value)
            fixed (TKey* pKey = &key)
            {
                return Add(pKey, pValue);
            }
        }



    }

}