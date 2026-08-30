using System.Runtime.InteropServices;

namespace Solamirare.Tests;


public static unsafe class ValueStack_Test
{
    public static bool Base()
    {
        ValueStack<int> stack = new ValueStack<int>(5);

        int size = sizeof(ValueStack<int>);

        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        stack.Push(4);
        stack.Push(5);


        bool result = stack.Capacity == 5 && stack.Count == 5;
        result = result && stack.SegmentCount == 1;


        bool p6 = stack.Push(6);


        result = result && p6;
        result = result && stack.SegmentCount == 2;


        stack.Dispose();


        return result;
    }

    public static bool PopAndPeekAndEmpty()
    {
        ValueStack<int> stack = new ValueStack<int>(3);

        // Test IsEmpty on new stack
        if (!stack.IsEmpty) { stack.Dispose(); return false; }

        stack.Push(10);
        stack.Push(20);
        stack.Push(30);

        // Test IsEmpty on non-empty stack
        if (stack.IsEmpty || stack.Count != 3) { stack.Dispose(); return false; }

        // Test Peek
        bool peekResult = stack.TryPeek(out int* peekedValue);
        if (!peekResult || *peekedValue != 30 || stack.Count != 3)
        {
            stack.Dispose();
            return false;
        }

        // Test Pop
        bool popResult = stack.TryPop(out int* poppedValue);
        if (!popResult || *poppedValue != 30 || stack.Count != 2)
        {
            stack.Dispose();
            return false;
        }

        // Pop remaining items
        stack.TryPop(out poppedValue); // 20
        if (*poppedValue != 20) { stack.Dispose(); return false; }

        stack.TryPop(out poppedValue); // 10
        if (*poppedValue != 10) { stack.Dispose(); return false; }

        // Test on empty stack
        if (stack.Count != 0 || !stack.IsEmpty) { stack.Dispose(); return false; }

        bool emptyPeek = stack.TryPeek(out _);
        bool emptyPop = stack.TryPop(out _);

        bool finalResult = !emptyPeek && !emptyPop;

        stack.Dispose();
        return finalResult;
    }

    public static bool ClearAndContains()
    {
        ValueStack<int> stack = new ValueStack<int>(10);
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        bool contains3 = stack.Contains(3);
        bool contains5 = stack.Contains(5);

        if (!contains3 || contains5)
        {
            stack.Dispose();
            return false;
        }

        stack.Clear();

        if (stack.Count != 0 || stack.Contains(3))
        {
            stack.Dispose();
            return false;
        }

        // Can still push after clearing
        stack.Push(100);
        if (stack.Count != 1 || !stack.Contains(100))
        {
            stack.Dispose();
            return false;
        }

        stack.Dispose();
        return true;
    }

    public static bool MultipleSegments()
    {
        const int initialCapacity = 4;
        const int totalItems = 10;
        ValueStack<int> stack = new ValueStack<int>(initialCapacity);

        for (int i = 0; i < totalItems; i++)
        {
            stack.Push(i);
        }

        if (stack.Count != totalItems || stack.SegmentCount <= 1)
        {
            stack.Dispose();
            return false;
        }

        // Verify LIFO order
        for (int i = totalItems - 1; i >= 0; i--)
        {
            if (!stack.TryPop(out int* val) || *val != i)
            {
                stack.Dispose();
                return false;
            }
        }

        if (stack.Count != 0)
        {
            stack.Dispose();
            return false;
        }

        stack.Dispose();
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TestStruct
    {
        public int A;
        public double B;

        public bool Equals(TestStruct other) => A == other.A && B == other.B;
    }

    public static bool StructTypeTest()
    {
        ValueStack<TestStruct> stack = new ValueStack<TestStruct>(2);

        TestStruct item1 = new TestStruct { A = 1, B = 1.1 };
        TestStruct item2 = new TestStruct { A = 2, B = 2.2 };
        TestStruct item3 = new TestStruct { A = 3, B = 3.3 };

        stack.Push(item1);
        stack.Push(item2);
        stack.Push(item3); // Should trigger new segment

        bool result = stack.Count == 3 && stack.SegmentCount == 2;

        result &= stack.TryPop(out TestStruct* val3) && val3->Equals(item3);
        result &= stack.TryPop(out TestStruct* val2) && val2->Equals(item2);
        result &= stack.TryPop(out TestStruct* val1) && val1->Equals(item1);
        result &= stack.Count == 0;

        stack.Dispose();
        return result;
    }

    public static bool EnumeratorTest()
    {
        ValueStack<int> stack = new ValueStack<int>(5);
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        int[] expectedOrder = { 1, 2, 3 };
        int index = 0;
        foreach (int* item in stack)
        {
            if (index >= expectedOrder.Length || *item != expectedOrder[index++])
            {
                stack.Dispose();
                return false;
            }
        }


        if (index != expectedOrder.Length)
        {
            stack.Dispose();
            return false;
        }


        if (stack.Count != 3)
        {
            stack.Dispose();
            return false;
        }

        stack.Dispose();
        return true;
    }

    public static bool TryCopyToTest()
    {
        ValueStack<int> stack = new ValueStack<int>(5);
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        int* array = stackalloc int[5];

        bool copyResult = stack.TryCopyTo(array, 0, 3);

        if (!copyResult)
        {
            stack.Dispose();
            return false;
        }

        if (array[0] != 1 || array[1] != 2 || array[2] != 3)
        {
            stack.Dispose();
            return false;
        }

        Span<int> smallArray = stackalloc int[2];
        smallArray[0] = 999;
        smallArray[1] = 999;

        bool smallCopyResult = stack.TryCopyTo(smallArray, 0, 3);

        if (smallCopyResult || smallArray[0] == 1 || smallArray[1] == 2)
        {
            stack.Dispose();
            return false;
        }

        int* offsetArray = stackalloc int[5];
        bool offsetCopyResult = stack.TryCopyTo(offsetArray, 1, 3);

        if (!offsetCopyResult || offsetArray[1] != 1 || offsetArray[2] != 2 || offsetArray[3] != 3)
        {
            stack.Dispose();
            return false;
        }


        stack.Dispose();
        return true;
    }


}
