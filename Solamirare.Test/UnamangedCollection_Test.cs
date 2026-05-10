
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Solamirare.Tests;



public static unsafe class UnamangedCollection_Test
{
    public static bool IndexOf_SIMD()
    {
        UnManagedMemory<char> mem1 = "abcdefghijklnmopqrst".AsSpan().CopyToUnManagedMemory();
        UnManagedMemory<char> mem2 = "abcdefghijk".AsSpan().CopyToUnManagedMemory();

        bool result_1 = ValueTypeHelper.IndexOf(mem1.AsSpan(), mem2.AsSpan()) == 0;
        bool result_2 = ValueTypeHelper.IndexOf(mem1.AsSpan(), "defgh".AsSpan()) == 3;

        mem1.Dispose();
        mem2.Dispose();

        return result_1 && result_2;

    }


    public static bool @foraech()
    {
        UnManagedMemory<int> mem = new UnManagedMemory<int>();

        mem.Add(1);
        mem.Add(2);
        mem.Add(3);
        mem.Add(4);
        mem.Add(5);

        int sum = 0;

        UnManagedCollection<int> col = mem.AsUnManagedCollection();

        foreach(int* i in col)
        {
            sum += *i;
        }

        mem.Dispose();

        return sum == 15;

    }
}
