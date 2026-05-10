using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Solamirare.Test;



public unsafe class MultiThreads_Test
{

    /// <summary>
    /// 常规性测试
    /// </summary>
    public unsafe class Common_Test
    {
        enum XYZ
        {
            X, Y, Z,
        }


        struct UEnum
        {
            public int Value;

            public XYZ xyz;
        }

        /// <summary>
        /// 测试按照bytes视图计算对象
        /// </summary>
        /// <returns></returns>
        public static bool BytesView()
        {
            UEnum u = new UEnum { Value = 77886655, xyz = XYZ.Y };

            Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref u, 1));

            bool lengthEquals = bytes.Length == sizeof(UEnum);

            UEnum u2 = MemoryMarshal.Read<UEnum>(bytes);

            UEnum u3 = MemoryMarshal.Cast<byte, UEnum>(bytes)[0];

            UEnum u4 = Unsafe.Read<UEnum>(Unsafe.AsPointer(ref bytes[0]));

            UEnum u5 = Unsafe.ReadUnaligned<UEnum>(ref bytes[0]);

            UEnum u6 = new UEnum { };

            UEnum u7 = new UEnum { };

            UEnum u8 = new UEnum { };

            UEnum u9 = new UEnum { };

            fixed (byte* p = &bytes[0])
            {
                u6 = *(UEnum*)p;

                Buffer.MemoryCopy(p, &u7, sizeof(UEnum), sizeof(UEnum));

                NativeMemory.Copy(p, &u8, (nuint)sizeof(UEnum));

                Unsafe.CopyBlock(&u9, p, (uint)sizeof(UEnum));


            }

            return lengthEquals
            && u2.Value == u.Value
            && u2.xyz == u.xyz
            && u3.Value == u.Value && u3.xyz == u.xyz
            && u4.Value == u.Value && u4.xyz == u.xyz
            && u5.Value == u.Value && u5.xyz == u.xyz
            && u6.Value == u.Value && u6.xyz == u.xyz
            && u7.Value == u.Value && u7.xyz == u.xyz
            && u8.Value == u.Value && u8.xyz == u.xyz
            && u9.Value == u.Value && u9.xyz == u.xyz
            ;
        }


        /// <summary>
        /// 测试在回调方法的使用过程中使用内联方法是否会造成 GC
        /// </summary>
        /// <returns></returns>
        public static bool UseInnerMethod()
        {
            ValueLinkedList<UnManagedMemory<char>> list = new ValueLinkedList<UnManagedMemory<char>>();

            var item_0 = new UnManagedMemory<char>("0000");
            var item_1 = new UnManagedMemory<char>("1111");

            list.Append(&item_0);
            list.Append(&item_1);


            // 当前这样传入静态方法不会造成 GC
            // 但是如果把 update 方法改成内联方法， 会产生 3 次 GC， 64 * 3 = 192 B 
            _useMethod(&list, update);
            _useMethod(&list, update);
            _useMethod(&list, update);


            item_0.Dispose();
            item_1.Dispose();
            list.Dispose();


            return true;
        }


        static uint update(UnManagedMemory<char>* i)
        {
            i->Update("3333");
            return i->UsageSize;
        }

        delegate uint eachItem(UnManagedMemory<char>* item);



        static void _useMethod(ValueLinkedList<UnManagedMemory<char>>* list, eachItem each)
        {
            var node = list->Index(0);
            uint length = each(node);

        }


        /// <summary>
        /// 检测反射是否会造成GC
        /// </summary>
        /// <returns></returns>
        public static bool CheckReflectionGC()
        {
            UnManagedMemory<char> ob = new();

            Type t = typeof(UnManagedMemory<char>);

            ReadOnlySpan<char> name = t.FullName;



            // //FieldInfo[] g = t.GetFields();
            // IEnumerable<FieldInfo> rfs = t.GetRuntimeFields();

            ob.Dispose();

            return true;
        }


        /// <summary>
        /// 测试不使用指针，而是传递对象的方式传递 UnManagedMemory 是否会造成 GC
        /// </summary>
        /// <returns></returns>
        public static bool TransferUnManagedMemory()
        {
            UnManagedMemory<char> obj = new UnManagedMemory<char>("my name");

            bool result = _receiveUnManagedMemory(obj);

            obj.Dispose();

            return result;
        }

        static bool _receiveUnManagedMemory(UnManagedMemory<char> obj)
        {
            uint length = obj.UsageSize;

            return length == 7 && obj.Equals("my name");
        }

        /// <summary>
        /// 测试生成随机字符串
        /// </summary>
        /// <returns></returns>
        public static bool RND_String()
        {

            uint desiredLength = 48;

            char* buffer = stackalloc char[(int)desiredLength];

            bool result = RandomStringGenerator.Generate(buffer, desiredLength);

            ReadOnlySpan<char> resultSpan = new ReadOnlySpan<char>(buffer, (int)desiredLength);

            UnManagedMemory<char> result2 = RandomStringGenerator.Generate(10);

            result2.Dispose();

            //因为是随机生成，没法验证结果，只能在 debug 状态人工查看生成的字符串

            return result;
        }


        /// <summary>
        /// 测试多字段赋值的便利使用
        /// </summary>
        /// <returns></returns>
        public static bool MultiFiledsStruct()
        {

            RequestUsers r = new RequestUsers
            {
                UserName = "name".CopyToChars(),
                Password = "password".CopyToChars(),
                Address = "address".CopyToChars(),
                Age = 0
            };

            r.Dispose();

            //=======================

            //char* memory = stackalloc char[19];
            char* memory = (char*)NativeMemory.Alloc((nuint)sizeof(char) * 19);

            RequestUsers r2 = new RequestUsers
            {
                UserName = "name".AsSpan().CopyToUnManagedMemory(memory),
                Password = "password".AsSpan().CopyToUnManagedMemory(memory + 4),
                Address = "address".AsSpan().CopyToUnManagedMemory(memory + 12),
                Age = 0
            };

            bool result = r2.Address.Equals("address");

            result = result && r2.UserName.Equals("name");

            result = result && r2.Password.Equals("password");

            NativeMemory.Free(memory);

            return result;

        }







        /// <summary>
        /// 测试class的长度与指针
        /// </summary>
        public static bool ClassSizeAndPointer()
        {
            SizeClass sc = new SizeClass();

            int size = sizeof(SizeClass); //长度是8，得到的是指针的长度，而不是对象的长度

            SizeClass* p_sc = &sc;



            // 1. 获取对象的地址（该地址指向方法表指针）
            void** pObj = (void**)p_sc;

            // 2. 获取方法表指针 (MethodTable*)
            MethodTable* pMT = (MethodTable*)*pObj;

            // 3. 获取并打印对象头信息
            long objectHeader = *((long*)p_sc - 1);

            Console.WriteLine($"运行时环境架构: {(IntPtr.Size == 8 ? "64-bit" : "32-bit")}");
            Console.WriteLine("------------------------------------------");
            Console.WriteLine($"[对象实例信息]");
            Console.WriteLine($"对象的固定地址: 0x{((nint)(p_sc)).ToInt64():X16}");
            Console.WriteLine($"对象头 (Object Header) 的值: 0x{objectHeader:X16}");
            Console.WriteLine();

            // 4. 解析并打印方法表 (MethodTable) 的信息
            Console.WriteLine("[方法表 (MethodTable) 解析]");
            Console.WriteLine($"方法表地址: 0x{(long)pMT:X16}");
            Console.WriteLine($"  - Flags: 0x{pMT->Flags:X8}");
            Console.WriteLine($"  - BaseSize: {pMT->BaseSize} 字节");
            Console.WriteLine($"  - 父类方法表地址: 0x{(long)pMT->ParentMethodTable:X16}");
            Console.WriteLine();

            // // 5. 打印父类方法表的基本信息
            // MethodTable* pParentMT = pMT->ParentMethodTable;

            // // 所有类型最终都继承自 System.Object，其父类为 null
            // if (pParentMT != null)
            // {
            //     Console.WriteLine("[父类方法表 (Parent MethodTable) 解析]");
            //     Console.WriteLine($"  - 父类方法表地址: 0x{(long)pParentMT:X16}");
            //     Console.WriteLine($"  - 父类 BaseSize: {pParentMT->BaseSize} 字节");
            //     Console.WriteLine();
            // }

            // 6. 遍历并打印虚方法表 (v-table) 中的方法槽
            // 方法槽从方法表地址的某个偏移量开始
            // 我们将打印前 10 个槽位作为示例
            Console.WriteLine("[虚方法表 (v-table) 槽位探索 - 前10个]");
            byte* pMethodSlots = (byte*)pMT + MethodTable.VTableSlotsOffset;

            for (int i = 0; i < 10; i++)
            {
                // 每个槽位是一个指针，指向一个方法描述符 (MethodDesc)
                IntPtr methodDescPtr = ((IntPtr*)pMethodSlots)[i];
                Console.WriteLine($"  - 槽位 #{i}: 0x{methodDescPtr.ToInt64():X16} (指向 MethodDesc)");
            }
            Console.WriteLine("  - ...");
            Console.WriteLine("注意：这些槽位包括继承自基类的方法、接口方法等。");


            return true;
        }



    }


    // 模拟 .NET 运行时内部的 MethodTable 结构 (简化版)
    // 注意：这个结构的具体布局取决于 .NET 版本和平台
    // 这个布局是基于 .NET 6/8 在 x64 上的观察
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct MethodTable
    {
        // 各种标志位
        [FieldOffset(0x00)]
        public uint Flags;

        // 对象实例的基本大小
        [FieldOffset(0x04)]
        public uint BaseSize;

        // 省略中间的许多字段...

        // 指向父类方法表的指针
        // 这个偏移量在不同版本间变化较大。0x40 是一个在 .NET 6/8 x64 上常见的偏移量
        [FieldOffset(0x40)]
        public MethodTable* ParentMethodTable;

        // ... 后面还有很多其他字段，例如指向 EEClass 的指针等

        // 从 MethodTable 地址开始，在某个固定偏移之后就是方法槽（v-table）的开始
        // 这个偏移量通常是 MethodTable 结构体自身大小之后。
        // 在 64 位系统上，一个指针是 8 字节。我们假设前 10 个指针大小的空间是 MethodTable 的固定字段。
        // 这只是一个用于演示的近似值！
        public const int VTableSlotsOffset = 10 * 8; // 80 bytes
    }





    internal class SizeClass
    {
        public int A;

        public int B;

        public int C;

        public int D;
    }






    public unsafe struct RequestUsers
    {
        public UnManagedMemory<char> UserName;

        public UnManagedMemory<char> Password;

        public UnManagedMemory<char> Address;

        public int Age;

        public void Dispose()
        {
            UserName.Dispose();
            Password.Dispose();
            Address.Dispose();
        }
    }
}
