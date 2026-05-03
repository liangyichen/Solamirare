namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{

    /// <summary>
    /// 描述当前对象状态的哈希码。
    /// </summary>
    public ulong StatusCode
    {
        get
        {
            fixed(ValueFrozenStack<T>* p = &this)
            {
                ulong result = Fingerprint.MemoryFingerprint(p);

                return result;
            }
        }
    }


}