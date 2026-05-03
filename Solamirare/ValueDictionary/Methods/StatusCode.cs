namespace Solamirare
{

    public unsafe partial struct ValueDictionary<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
    {
        /// <summary>
        /// 描述当前对象状态的哈希码。
        /// </summary>
        public ulong StatusCode
        {
            get
            {
                fixed (ValueDictionary<TKey, TValue>* p = &this)
                {
                    ulong result = Fingerprint.MemoryFingerprint(p);

                    return result;
                }
            }
        }

    }
}