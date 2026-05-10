using System.Runtime.InteropServices;

namespace Solamirare.Test
{
    public unsafe class ValueFrozenStackTests
    {
        public static bool TestConstruction()
        {
            uint capacity = 128;
            // onMemoryPool = false 确保使用 NativeMemory，不依赖外部 PoolCluster
            ValueFrozenStack<int> stack = new ValueFrozenStack<int>(capacity, false);

            if (stack.Capacity != capacity) return false;
            // 根据当前实现，初始 FreeNodesCount 为 0
            if (stack.Count != 0) return false;

            stack.Dispose();

            return true;
        }

        public static bool TestExternalMemory()
        {
            uint capacity = 64;
            nuint byteCount = (nuint)capacity * (nuint)sizeof(int);
            // 自身创建资源
            int* memory = (int*)NativeMemory.AllocZeroed(byteCount);


            ValueFrozenStack<int> stack = new ValueFrozenStack<int>(memory, capacity);

            if (stack.Capacity != capacity) return false;
            if (stack.Count != 0) return false;


            stack.Dispose();

            return true;

        }

        public static bool TestZeroCapacity()
        {
            ValueFrozenStack<int> stack = new ValueFrozenStack<int>(0);
            if (stack.Capacity != 0) return false;


            stack.Dispose();
            return true;
        }


        public static bool TestPushPop()
        {
            ValueFrozenStack<int> stack = new ValueFrozenStack<int>(16);

            stack.Push(100);
            stack.Push(200);
            stack.Push(300);

            // 验证数量增加
            if (stack.Count != 3) return false;

            // 验证 LIFO 顺序
            if (stack.Pop() != 300) return false;
            if (stack.Pop() != 200) return false;
            if (stack.Pop() != 100) return false;

            if (stack.Count != 0) return false;

            stack.Dispose();
            return true;
        }

        public static bool TestClear()
        {
            ValueFrozenStack<int> stack = new ValueFrozenStack<int>(16);

            stack.Push(1);
            stack.Push(2);

            stack.Clear();

            if (stack.Count != 0) return false;

            stack.Dispose();
            return true;
        }
    }
}