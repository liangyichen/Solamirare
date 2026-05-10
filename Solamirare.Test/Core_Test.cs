namespace Solamirare.Tests;

public static class Core_Tests
{



    /// <summary>
    /// 压测 Align 对齐
    /// 覆盖边界：0, 1, 3, 9, 40, 63, 64, 65, 大数
    /// </summary>
    public static bool VerifyAlignmentLogic()
    {
        // 测试用例定义：(输入值, 期望结果)
        var testCases = new (int Input, int Expected)[]
        {
            (0, 0),             // 0 对齐仍为 0
            (1, 64),            // 1 向上取整到 64
            (3, 64),            // 3 向上取整到 64
            (9, 64),            // 9 向上取整到 64
            (40, 64),           // 40 向上取整到 64
            (63, 64),           // 63 向上取整到 64
            (64, 64),           // 已经是 64，保持不变
            (65, 128),          // 65 向上取整到 128
            (127, 128),         // 127 向上取整到 128
            (128, 128),         // 已经是 128，保持不变
            (1000, 1024),       // 1000 向上取整到 1024 (64*16)
            (1048576, 1048576)  // 1MB 已经是 64 的倍数，保持不变
        };

        foreach (var (input, expected) in testCases)
        {
            int result = MemoryAlignmentHelper.Align(input);

            // 如果任何一个计算结果不符合预期，立即返回 false
            if (result != expected)
            {
                // 调试输出（可选）: 
                // Console.WriteLine($"失败! 输入:{input}, 期望:{expected}, 实际:{result}");
                return false;
            }
        }

        // 验证指针对齐逻辑
        if (!VerifyPointerAlignment()) return false;

        return true; // 全部通过
    }

    private static unsafe bool VerifyPointerAlignment()
    {
        // 模拟一个完全不对齐的地址 0x1
        byte* rawPtr = (byte*)1;

        // 指针对齐逻辑：(ptr + 63) & ~63
        // 使用 nuint 确保在 64 位环境下位运算安全
        byte* alignedPtr = (byte*)(((nuint)rawPtr + 63) & ~(nuint)63);

        // 期望结果应该是 64 (0x40)
        return (long)alignedPtr == 64;
    }

 
 
public static unsafe bool Test_Count_Int_Alignment_Stress()
{
    uint elementCount = 64; // 256 字节
    var source = new UnManagedMemory<int>(elementCount, elementCount);
    var target = new UnManagedMemory<int>(1, 1);

    try
    {
        byte* pBytes = (byte*)source.Pointer;
        // 全填充 0
        for (int i = 0; i < 256; i++) pBytes[i] = 0;

        // 目标值 0x12345678
        int targetValue = 0x12345678;
        ((int*)target.Pointer)[0] = targetValue;

        // 1. 在对齐位写入 3 个匹配 (索引 0, 10, 60)
        ((int*)source.Pointer)[0] = targetValue;
        ((int*)source.Pointer)[10] = targetValue;
        ((int*)source.Pointer)[60] = targetValue;

        // 2. 在非对齐位注入“剧毒”干扰
        // 在字节偏移 7 处写入 0x12345678
        // 这会跨越 索引 1 和 2 的 int 边界，但它不是有效的 int 匹配
        pBytes[7] = 0x78;
        pBytes[8] = 0x56;
        pBytes[9] = 0x34;
        pBytes[10] = 0x12;

        int result = ValueTypeHelper.Count(source.Pointer, elementCount, target.Pointer, 1);

        // 如果掩码过滤有效，结果必须是 3。如果是 4，说明过滤失败。
        return result == 3;
    }
    finally
    {
        source.Dispose();
        target.Dispose();
    }
}


 
public static unsafe bool Test_Count_Overlapping_Stress()
{
    // 构造 40 个 'A'
    string data = new string('A', 40); 
    var source = new UnManagedMemory<char>(data);
    var target = new UnManagedMemory<char>("AA");

    try
    {
        // 理论计算：
        // 长度为 N 的序列搜长度为 M 的模式，重叠匹配数 = N - M + 1
        // 40 - 2 + 1 = 39
        int result = ValueTypeHelper.Count(source.Pointer, 40, target.Pointer, 2);

        return result == 39;
    }
    finally
    {
        source.Dispose();
        target.Dispose();
    }
}



public static unsafe bool Test_Count_Cross_Window_Boundary()
{
    // 构造一个 64 字符 (128 字节) 的环境
    // 在第 15 个字符处放入 "AB"，其字节偏移为 30, 31, 32, 33
    // 恰好跨越了第一个 Vector256 (0-31字节) 和第二个 Vector256 (32-63字节)
    string data = new string('.', 64); 
    var source = new UnManagedMemory<char>(data);
    var target = new UnManagedMemory<char>("AB");

    try
    {
        char* pS = (char*)source.Pointer;
        // 清理内存
        for (int i = 0; i < 64; i++) pS[i] = '.';
        
        // 放置跨界模式
        pS[15] = 'A'; // 字节偏移 30, 31
        pS[16] = 'B'; // 字节偏移 32, 33

        int result = ValueTypeHelper.Count(source.Pointer, 64, target.Pointer, 2);

        // 如果 Count_Ultra 只是机械地按 32 字节块扫描首字节，
        // 它会发现偏移 30 是 'A'，然后去比对后续，逻辑应该能闭环。
        return result == 1;
    }
    finally
    {
        source.Dispose();
        target.Dispose();
    }
}

    public static unsafe bool Test_Count_Complex_Overlap()
    {
        // 测试目标：在长文本中统计重复出现的模式串
        // 构造： "AAAA" 在 "AAAAAAAA" 中应出现 5 次 (0,1,2,3,4)
        uint size = 8;
        var source = new UnManagedMemory<byte>(size, size);
        var target = new UnManagedMemory<byte>(4, 4);

        try
        {
            for (int i = 0; i < 8; i++) source.Pointer[i] = (byte)'A';
            for (int i = 0; i < 4; i++) target.Pointer[i] = (byte)'A';

            int result = ValueTypeHelper.Count(source.Pointer, 8, target.Pointer, 4);

            if (result != 5) return false;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }

        return true;
    }


    public static unsafe bool Test_Count_Ultra_Large_Char()
    {
        uint elementCount = 40; // 160 字节
        var source = new UnManagedMemory<int>(elementCount, elementCount);
        var target = new UnManagedMemory<int>(1, 1);

        try
        {
            int* pSource = (int*)source.Pointer;
            // 填充干扰数据
            for (int i = 0; i < 40; i++) pSource[i] = 123;

            // 在特定位置插入 4 个目标值
            pSource[0] = 888;
            pSource[15] = 888;
            pSource[31] = 888; // 跨越 32 字节边界的位置
            pSource[39] = 888; // 末尾位置

            int* pTarget = (int*)target.Pointer;
            pTarget[0] = 888;

            int result = ValueTypeHelper.Count(source.Pointer, elementCount, target.Pointer, 1);

            return result == 4;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }



    /// <summary>
    /// 测试 IndexOf 的基础匹配与未匹配情况 (sizeOfValue = 1)
    /// </summary>
    public static unsafe bool Test_IndexOf_Basic()
    {


        ReadOnlySpan<byte> source = "Mississippi"u8;

        ReadOnlySpan<byte> target = "ssi"u8;

        ReadOnlySpan<byte> none = "apple"u8;


        fixed (byte* pSource = source)
        fixed (byte* pTarget = target)
        fixed (byte* pNone = none)
        {
            int r1 = ValueTypeHelper.IndexOf_Short_Bytes(pSource, (int)source.Length, pTarget, (int)target.Length, 1);
            int r2 = ValueTypeHelper.IndexOf_Short_Bytes(pSource, (int)source.Length, pNone, (int)none.Length, 1);
            return r1 == 2 && r2 == -1;
        }

    }

    public static unsafe bool Test_IndexOf_Short_Edge_Constraint()
    {
        // 分配 32 字节 (一个向量)
        uint size = 32;
        var source = new UnManagedMemory<byte>(size, size);
        var target = new UnManagedMemory<byte>(4, 4); // "EDGE"

        try
        {
            // 在索引 0 处放一个干扰项 "EDGF"
            source.Pointer[0] = (byte)'E'; source.Pointer[1] = (byte)'D'; source.Pointer[2] = (byte)'G'; source.Pointer[3] = (byte)'F';

            // 在索引 28 处放真正的 "EDGE" (刚好在 32 字节末尾)
            byte[] real = "EDGE"u8.ToArray();
            fixed (byte* pR = real)
            {
                Buffer.MemoryCopy(pR, target.Pointer, 4, 4);
                Buffer.MemoryCopy(pR, source.Pointer + 28, 4, 4);
            }

            // 关键：我们将 sourceLen 限制为 32。
            // IndexOf_Short_Bytes 应该跳过开头的干扰，准确返回 28。
            int result = ValueTypeHelper.IndexOf_Short_Bytes(source.Pointer, 32, target.Pointer, 4, 1);

            return result == 28;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_IndexOf_Short_CacheLine_Stress()
    {
        // 分配 64 字节，模拟两个 32 字节向量窗口
        uint size = 64;
        var source = new UnManagedMemory<byte>(size, size);
        var target = new UnManagedMemory<byte>(4, 4); // 模式串: "ABCD"

        try
        {
            // 1. 在整个 source 中填满 'A' (伪首字节干扰)
            for (uint i = 0; i < size; i++) source.Pointer[i] = (byte)'A';

            // 2. 准备真正的模式串 "ABCD"
            byte[] pData = "ABCD"u8.ToArray();
            fixed (byte* pP = pData) Buffer.MemoryCopy(pP, target.Pointer, 4, 4);

            // 3. 将真实匹配放在偏移 30 处 (跨越 32 字节 Block 边界)
            // 索引 30,31 是 'A','B'; 索引 32,33 是 'C','D'
            Buffer.MemoryCopy(target.Pointer, source.Pointer + 30, 4, 4);

            // 注意：Short_Bytes 只能扫描第一个 32 字节窗口。
            // 如果我们传 sourceLen = 34，它应该能通过第一个 Vector256 探测到起始于 30 的匹配
            int result = ValueTypeHelper.IndexOf_Short_Bytes(source.Pointer, 34, target.Pointer, (int)target.UsageSize, 1);

            return result == 30;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }



    /// <summary>
    /// 测试 LastIndexOf 的逆向扫描逻辑 (MSB -> LSB)
    /// </summary>
    public static unsafe bool Test_LastIndexOf_Basic()
    {
        var source = new UnManagedMemory<byte>("banana"u8);
        var target = new UnManagedMemory<byte>("ana"u8);
        try
        {
            // "ana" 出现于索引 1 和 3，LastIndexOf 应返回 3
            int r1 = ValueTypeHelper.LastIndexOf_Short_Bytes(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);
            return r1 == 3;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    /// <summary>
    /// 测试 StartsWith 和 EndsWith 的 Overlapping 逻辑
    /// </summary>
    public static unsafe bool Test_StartsWith_EndsWith()
    {
        var source = new UnManagedMemory<byte>("TheQuickBrownFox"u8);
        var start = new UnManagedMemory<byte>("TheQu"u8);
        var end = new UnManagedMemory<byte>("wnFox"u8);
        var falseStart = new UnManagedMemory<byte>("Quick"u8);
        try
        {
            bool b1 = ValueTypeHelper.StartsWith(source.Pointer, (int)source.UsageSize, start.Pointer, (int)start.UsageSize);
            bool b2 = ValueTypeHelper.EndsWith(source.Pointer, (int)source.UsageSize, end.Pointer, (int)end.UsageSize);
            bool b3 = ValueTypeHelper.StartsWith(source.Pointer, (int)source.UsageSize, falseStart.Pointer, (int)falseStart.UsageSize);

            return b1 && b2 && !b3;
        }
        finally
        {
            source.Dispose();
            start.Dispose();
            end.Dispose();
            falseStart.Dispose();
        }
    }

    /// <summary>
    /// 测试空模式串和边界单字节匹配
    /// </summary>
    public static unsafe bool Test_Empty_And_Single_Byte()
    {
        var source = new UnManagedMemory<byte>("A"u8);
        var empty = new UnManagedMemory<byte>(Array.Empty<byte>());
        var single = new UnManagedMemory<byte>("A"u8);
        try
        {
            bool b1 = ValueTypeHelper.StartsWith(source.Pointer, (int)source.UsageSize, empty.Pointer, (int)empty.UsageSize);
            int r1 = ValueTypeHelper.IndexOf_Short_Bytes(source.Pointer, (int)source.UsageSize, empty.Pointer, (int)empty.UsageSize, 1);
            int r2 = ValueTypeHelper.IndexOf_Short_Bytes(source.Pointer, (int)source.UsageSize, single.Pointer, (int)single.UsageSize, 1);

            return b1 == true && r1 == 0 && r2 == 0;
        }
        finally
        {
            source.Dispose();
            empty.Dispose();
            single.Dispose();
        }
    }


    public static unsafe bool Test_IndexOf_Ultra_Heavy_Collision()
    {
        // 分配 1024 字节数据块
        uint size = 1024;
        var source = new UnManagedMemory<byte>(size, size);
        var target = new UnManagedMemory<byte>(8, 8); // 模式串 "AAAAAAAB"

        try
        {
            // 1. 整个 1KB 填满 'A' (这会导致每一处首字节扫描都匹配)
            for (uint i = 0; i < size; i++) source.Pointer[i] = (byte)'A';

            // 2. 准备模式串
            byte[] pData = "AAAAAAAB"u8.ToArray();
            fixed (byte* pP = pData) Buffer.MemoryCopy(pP, target.Pointer, 8, 8);

            // 3. 将真实匹配放在接近末尾的 900 处
            int expectedIndex = 900;
            Buffer.MemoryCopy(target.Pointer, source.Pointer + expectedIndex, 8, 8);

            // 4. 执行长模式 (sizeOfValue = 1)
            int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == expectedIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_IndexOf_Ultra_CrossVectorBoundary()
    {
        // 准备一个 64 字节的容器
        var source = new UnManagedMemory<byte>(64, 64);
        // 准备一个 8 字节的模式串
        var target = new UnManagedMemory<byte>("Target78"u8);

        try
        {
            // 将模式串放在偏移 28 的位置
            // 这意味着模式串的 [0..3] 在第一个 32 字节内，[4..7] 在第二个 32 字节内
            byte* pDest = source.Pointer + 28;
            byte[] data = "Target78"u8.ToArray();
            for (int i = 0; i < data.Length; i++) pDest[i] = data[i];

            // 执行长模式计算 (sizeOfValue = 1)
            int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == 28;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }


    public static unsafe bool Test_IndexOf_Ultra_Int_Stress()
    {
        // 2048 字节 = 512 个 int
        uint sizeInBytes = 2048;
        var source = new UnManagedMemory<byte>(sizeInBytes, sizeInBytes);

        // 模式串为两个 int: [0x11223344, 0x55667788]
        int[] pattern = { 0x11223344, 0x55667788 };
        var target = new UnManagedMemory<byte>(8, 8);
        fixed (int* pP = pattern) Buffer.MemoryCopy(pP, target.Pointer, 8, 8);

        try
        {
            // 1. 填充干扰：在每个对齐位填入首个 int (0x11223344)，但第二个 int 不匹配
            int* pSourceInt = (int*)source.Pointer;
            for (int i = 0; i < 512; i++) pSourceInt[i] = 0x11223344;

            // 2. 将唯一正确的完整匹配放在索引 450 (int 索引)
            int expectedCharIndex = 450;
            pSourceInt[expectedCharIndex] = pattern[0];
            pSourceInt[expectedCharIndex + 1] = pattern[1];

            // 3. 执行 (sizeOfValue = 4)
            int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, (int)sizeInBytes, target.Pointer, 8, 4);

            return result == expectedCharIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_IndexOf_Ultra_Misaligned_LongMatch()
    {
        // 分配 2048 字节
        uint total = 2048;
        var source = new UnManagedMemory<byte>(total, total);
        var target = new UnManagedMemory<byte>(8, 8);

        try
        {
            // 模式串: "LONGPATH"
            byte[] pData = "LONGPATH"u8.ToArray();
            fixed (byte* pP = pData) Buffer.MemoryCopy(pP, target.Pointer, 8, 8);

            // 故意让搜索起始指针不对齐 (偏移 13 字节)
            byte* pStart = source.Pointer + 13;
            uint searchLen = 1000;

            // 将匹配放在 pStart 之后的第 512 字节 (绝对偏移 525)
            int expectedRelIndex = 512;
            Buffer.MemoryCopy(target.Pointer, pStart + expectedRelIndex, 8, 8);

            // 测试 Ultra 路径是否能正确处理 pStart 不是 32 字节对齐的情况
            int result = ValueTypeHelper.IndexOf_Ultra(pStart, (int)searchLen, target.Pointer, 8, 1);

            return result == expectedRelIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_IndexOf_Ultra_PageBoundary_Safety()
    {
        // 分配稍微多一点，但只在靠近边界的地方搜索
        uint size = 1024;
        var source = new UnManagedMemory<byte>(size, size);
        var target = new UnManagedMemory<byte>(16, 16);

        try
        {
            // 模式串
            for (int i = 0; i < 16; i++) target.Pointer[i] = 0xEE;

            // 将匹配放在索引 1000 处，后面只剩 24 字节。
            // 如果 IndexOf_Ultra 粗鲁地进行下一次 32 字节读取，可能会触发安全问题
            int expectedIndex = 1000;
            for (int i = 0; i < 16; i++) source.Pointer[expectedIndex + i] = 0xEE;

            int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, 1024, target.Pointer, 16, 1);

            return result == expectedIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }



    public static unsafe bool Test_IndexOf_Ultra_Unaligned_Entry()
    {
        uint totalBuf = 256;
        var sourceFull = new UnManagedMemory<byte>(totalBuf, totalBuf);
        var target = new UnManagedMemory<byte>(4, 4);

        try
        {
            // 模式串 "TEST"
            byte[] pData = "TEST"u8.ToArray();
            fixed (byte* pP = pData) Buffer.MemoryCopy(pP, target.Pointer, 4, 4);

            // 在绝对地址偏移 7 的位置开始搜索 (故意不对齐)
            byte* pStart = sourceFull.Pointer + 7;
            int searchLen = 100;

            // 将匹配放在 pStart 之后的第 50 个字节处
            Buffer.MemoryCopy(target.Pointer, pStart + 50, 4, 4);

            // 调用时传入不对齐的指针
            int result = ValueTypeHelper.IndexOf_Ultra(pStart, searchLen, target.Pointer, (int)target.UsageSize, 1);

            return result == 50;
        }
        finally
        {
            sourceFull.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_IndexOf_Ultra_Prefix_Overlap_Stress()
    {
        uint dataSize = 512;
        var source = new UnManagedMemory<byte>(dataSize, dataSize);
        var target = new UnManagedMemory<byte>(8, 8); // 模式串: "AAAAAAAB"

        try
        {
            // 1. 填充干扰：全是 "AAAAAAAC" (前 7 位匹配，最后一位不匹配)
            for (uint i = 0; i < dataSize - 8; i += 8)
            {
                byte[] fake = "AAAAAAAC"u8.ToArray();
                fixed (byte* pF = fake) Buffer.MemoryCopy(pF, source.Pointer + i, 8, 8);
            }

            // 2. 在 480 字节处插入真身 "AAAAAAAB"
            byte[] real = "AAAAAAAB"u8.ToArray();
            int expectedIndex = 480;
            fixed (byte* pR = real)
            {
                Buffer.MemoryCopy(pR, target.Pointer, 8, 8);
                Buffer.MemoryCopy(pR, source.Pointer + expectedIndex, 8, 8);
            }

            int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == expectedIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_IndexOf_Ultra_LargeScale()
    {
        uint dataSize = 4096; // 4KB 数据
        var source = new UnManagedMemory<byte>(dataSize, dataSize);
        var target = new UnManagedMemory<byte>("FindMe!!"u8);

        try
        {
            // 在接近末尾的位置插入目标 (例如 4000)
            int expectedIndex = 4000;
            byte[] data = "FindMe!!"u8.ToArray();
            for (int i = 0; i < data.Length; i++) source.Pointer[expectedIndex + i] = data[i];

            int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == expectedIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }


    public static unsafe bool Test_IndexOf_Ultra_TailOverlap()
    {
        // 长度为 40 字节 (1个 32字节 Block + 8字节剩余)
        var source = new UnManagedMemory<byte>(40, 40);
        var target = new UnManagedMemory<byte>("TailMatch"u8);

        try
        {
            // 将匹配点放在最后，使其必须通过末尾 Overlapping 逻辑才能搜到
            int expectedIndex = 40 - (int)target.UsageSize; // 索引 31
            byte[] data = "TailMatch"u8.ToArray();
            for (int i = 0; i < data.Length; i++) source.Pointer[expectedIndex + i] = data[i];

            int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == expectedIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_IndexOf_Ultra_Char_Alignment()
    {
        // 128 字节，相当于 64 个 char
        var source = new UnManagedMemory<byte>(128, 128);
        // 搜索字符 "Ultra" (10 字节)
        ReadOnlySpan<char> pattern = "Ultra";
        fixed (char* pP = pattern)
        {
            var target = new UnManagedMemory<byte>((byte*)pP, (uint)pattern.Length * 2, (uint)pattern.Length * 2);
            try
            {
                // 在第 50 个字符处插入 (字节偏移 100)
                int charIndex = 50;
                byte* pDest = source.Pointer + (charIndex * 2);
                Buffer.MemoryCopy(target.Pointer, pDest, 10, 10);

                // sizeOfValue = 2
                int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 2);

                return result == charIndex;
            }
            finally
            {
                target.Dispose();
                source.Dispose();
            }
        }
    }

    public static unsafe bool Test_IndexOf_Ultra_NotFound()
    {
        var source = new UnManagedMemory<byte>(1024);
        var target = new UnManagedMemory<byte>("NoSuchString"u8);

        try
        {
            // 填充一些干扰数据
            for (int i = 0; i < 1024; i++) source.Pointer[i] = (byte)(i % 255);

            int result = ValueTypeHelper.IndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == -1;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_LastIndexOf_Ultra_CrossBlock()
    {
        // 准备 64 字节数据 (2 个 32 字节 Block)
        var source = new UnManagedMemory<byte>(64, 64);
        var target = new UnManagedMemory<byte>("Cross789"u8);

        try
        {
            // 将模式串放在偏移 28 的位置。
            // 这意味着 "Cross" 在前 32 字节，"789" 在后 32 字节。
            // 逆向扫描必须能从后向前扫描时，在跨越边界处准确锁定该位置。
            byte[] data = "Cross789"u8.ToArray();
            byte* pDest = source.Pointer + 28;
            for (int i = 0; i < data.Length; i++) pDest[i] = data[i];

            int result = ValueTypeHelper.LastIndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == 28;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }


    public static unsafe bool Test_LastIndexOf_Ultra_ReversePageBoundary()
    {
        uint size = 1024;
        // 分配 1024 字节并填满容量
        var source = new UnManagedMemory<byte>(size, size);
        var target = new UnManagedMemory<byte>(4, 4);

        try
        {
            byte[] pData = "LAST"u8.ToArray();
            fixed (byte* pP = pData) Buffer.MemoryCopy(pP, target.Pointer, 4, 4);

            // 将匹配放在索引 0 (最开头)
            // 逆向扫描必须完整走完所有 32 字节 Block，直到最开头的碎字节部分
            Buffer.MemoryCopy(target.Pointer, source.Pointer, 4, 4);

            int result = ValueTypeHelper.LastIndexOf_Ultra(source.Pointer, 1024, target.Pointer, 4, 1);

            return result == 0;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }



    public static unsafe bool Test_LastIndexOf_Ultra_HighFreq_Distraction()
    {
        uint size = 256;
        var source = new UnManagedMemory<byte>(size, size);
        var target = new UnManagedMemory<byte>(4, 4);

        try
        {
            // 模式串: "DEEF"
            byte[] real = "DEEF"u8.ToArray();
            fixed (byte* pR = real) Buffer.MemoryCopy(pR, target.Pointer, 4, 4);

            // 填充干扰：全是 "DEEE"
            for (uint i = 0; i < size - 4; i++) source.Pointer[i] = (i % 4 == 3) ? (byte)'E' : real[i % 4];

            // 在索引 50 和 200 处放入真身
            Buffer.MemoryCopy(target.Pointer, source.Pointer + 50, 4, 4);
            Buffer.MemoryCopy(target.Pointer, source.Pointer + 200, 4, 4);

            int result = ValueTypeHelper.LastIndexOf_Ultra(source.Pointer, 256, target.Pointer, 4, 1);

            return result == 200;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_LastIndexOf_Ultra_MultipleMatches()
    {
        uint dataSize = 1024; // 1KB 数据
        var source = new UnManagedMemory<byte>(dataSize, dataSize);
        var target = new UnManagedMemory<byte>("Match"u8);

        try
        {
            byte[] data = "Match"u8.ToArray();

            // 在索引 100 处放一个
            for (int i = 0; i < data.Length; i++) source.Pointer[100 + i] = data[i];

            // 在索引 900 处放一个（这才是 LastIndexOf 应该返回的）
            int expectedIndex = 900;
            for (int i = 0; i < data.Length; i++) source.Pointer[expectedIndex + i] = data[i];

            int result = ValueTypeHelper.LastIndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == expectedIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_LastIndexOf_Ultra_MSB_Priority()
    {
        var source = new UnManagedMemory<byte>(32, 32);
        var target = new UnManagedMemory<byte>("AB"u8);

        try
        {
            // 在同一个向量中构造两个匹配点
            // 索引 5: AB
            source.Pointer[5] = (byte)'A'; source.Pointer[6] = (byte)'B';

            // 索引 25: AB (LastIndexOf 应该返回这个)
            int expectedIndex = 25;
            source.Pointer[25] = (byte)'A'; source.Pointer[26] = (byte)'B';

            int result = ValueTypeHelper.LastIndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 1);

            return result == expectedIndex;
        }
        finally
        {
            source.Dispose();
            target.Dispose();
        }
    }

    public static unsafe bool Test_LastIndexOf_Ultra_Int_Alignment()
    {
        // 256 字节，相当于 64 个 int
        var source = new UnManagedMemory<byte>(256, 256);
        // 搜索 8 字节 (2 个 int)
        int[] pattern = new int[] { 0x12345678, 0x77889900 };

        fixed (int* pP = pattern)
        {
            var target = new UnManagedMemory<byte>((byte*)pP, 8, 8);
            try
            {
                // 在第 40 个 int 处插入 (字节偏移 160)
                int intIndex = 40;
                byte* pDest = source.Pointer + (intIndex * 4);
                Buffer.MemoryCopy(target.Pointer, pDest, 8, 8);

                // 故意在第 10 个 int 处也插入一个，测试是否返回最后面的那个
                byte* pEarlier = source.Pointer + (10 * 4);
                Buffer.MemoryCopy(target.Pointer, pEarlier, 8, 8);

                int result = ValueTypeHelper.LastIndexOf_Ultra(source.Pointer, (int)source.UsageSize, target.Pointer, (int)target.UsageSize, 4);

                return result == intIndex;
            }
            finally
            {
                target.Dispose();
                source.Dispose();
            }
        }
    }


}