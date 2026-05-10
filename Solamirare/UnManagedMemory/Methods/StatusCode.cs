namespace Solamirare;


public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{
    
    /// <summary>
    /// 描述当前对象的状态码(当前对象的各个属性或者字段发生改变，状态码会发生改变，但是间接指向内存的值发生改变时，当前状态码不会改变)
    /// </summary>
    public MemoryFingerprint128 StatusCode
    {
        get
        {
            if (!activated) return MemoryFingerprint128.Zero;
   
                MemoryFingerprint128 result;
                
                fixed(UnManagedMemory<T>* self = &this)
                result = MemoryFingerprint.Fingerprint(self);

                return result;
            
        }
    }

}

