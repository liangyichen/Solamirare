
using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{
    
    /// <summary>
    /// 描述当前对象状态的哈希码。
    /// </summary>
    public ulong StatusCode
    {
        get
        {
            if (!activated) return 0;

            fixed(UnManagedMemory<T>* p = &this)
            {
                ulong result = Fingerprint.MemoryFingerprint(p);

                return result;
            }
        }
    }

}

