
using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 当前状态码
    /// </summary>
    public ulong StatusCode
    {
        get
        {
            fixed (UnManagedCollection<T>* p = &this)
            {
                ulong result = Fingerprint.MemoryFingerprint(p);

                return result;
            }
        }
    }

}

