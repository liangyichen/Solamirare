using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Solamirare.Tests;

//一个48长度的结构，用于测试
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
public struct baseTestBox
{
    long p1, p2, p3, p4, p5, p6;
}

public static unsafe class MemoryCube_Test
{

    struct Transform_A
    {
        public int f1, f2, f3, f4, f5, f6, f7, f8, f9, f10;
    }


    struct Transform_B //占用内存总量与Transform_A相同，但是每个字段是Transform_A的字段的翻倍，所以字段数量是Transform_A的1/2
    {
        public long f1, f2, f3, f4, f5;
    }


    struct Transform_C
    {
        public long f1, f2, f3, f4, f5;
    }

    /// <summary>
    /// 尝试在结束使用一种类型后，再次利用对象以另一种类型工作
    /// </summary>
    /// <returns></returns>
    public static bool Transform()
    {

        MemoryObjectPool<int> memoryCube = new MemoryObjectPool<int>(10);

        memoryCube.Alloc(out void* m1, out uint length);
        *(int*)m1 = 1;
        memoryCube.Alloc(out void* m2, out uint length2);
        *(int*)m2 = 2;
        memoryCube.Alloc(out void* m3, out uint length3);
        *(int*)m3 = 3;
        memoryCube.Alloc(out void* m4, out uint length4);
        *(int*)m4 = 4;
        memoryCube.Alloc(out void* m5, out uint length5);
        *(int*)m5 = 5;
        memoryCube.Alloc(out void* m6, out uint length6);
        *(int*)m6 = 6;
        memoryCube.Alloc(out void* m7, out uint length7);
        *(int*)m7 = 7;
        memoryCube.Alloc(out void* m8, out uint length8);
        *(int*)m8 = 8;
        memoryCube.Alloc(out void* m9, out uint length9);
        *(int*)m9 = 9;
        memoryCube.Alloc(out void* m10, out uint length10);
        *(int*)m10 = 10;


        bool full = memoryCube.FreeNodesCount == 10;

        memoryCube.Dispose();

        memoryCube.Reconstruct<Transform_C>(1);

        memoryCube.Alloc(out void* b0, out uint length11);

        Transform_C* p_c = (Transform_C*)b0;

        p_c->f1 = 100;
        p_c->f2 = 200;
        p_c->f3 = 333;
        p_c->f4 = 444;
        p_c->f5 = 555;


        return true;

    }

    /// <summary>
    /// 基础测试
    /// </summary>
    /// <returns></returns>
    public static bool BaseTest()
    {

        MemoryObjectPool<baseTestBox> memoryCube = new MemoryObjectPool<baseTestBox>(5);

        memoryCube.Alloc(out void* m1, out uint length12); //0x000000011ce2fb40

        memoryCube.Alloc(out void* m2, out uint length13); //0x000000011ce2fb10

        void* p_m2 = m2;
        memoryCube.Alloc(out void* m3, out uint length14); //0x000000011ce2fae0

        memoryCube.Alloc(out void* m4, out uint length15); //0x000000011ce2fab0
        void* p_m4 = m4;

        memoryCube.Alloc(out void* m5, out uint length16); //0x000000011ce2fa80
        void* p_m5 = m5;

        //m2,m4, 空闲出不连续的内存段
        memoryCube.Return(m2);

        memoryCube.Return(m4);

        memoryCube.Return(m5);

        memoryCube.Alloc(out void* m21, out uint length17); //0x000000011ce2fa80 ，原 m5

        memoryCube.Alloc(out void* m22, out uint length18); //0x000000011ce2fab0 ，原 m4

        memoryCube.Alloc(out void* m23, out uint length19); //0x000000011ce2fb10 ，原 m2

        //因为内部通过栈存储，后进先出型算法，所以最先需求的是最后存储的
        bool result_0 = m21 == p_m5 && m22 == p_m4 && m23 == p_m2;

        memoryCube.Dispose();


        return result_0;

    }
}