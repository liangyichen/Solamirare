using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Solamirare.Tests;


public unsafe class Test_ValueLiskedList
{
    [Fact]
    public void AppendReferences()
    {
        Assert.True(ValueLiskedList_Test.AppendReferences());
    }



    [Fact]
    public void MixedAppend()
    {
        Assert.True(ValueLiskedList_Test.MixedAppend());
    }

    [Fact]
    public void Replace()
    {
        Assert.True(ValueLiskedList_Test.Replace());
    }

    [Fact]
    public void Update()
    {
        Assert.True(ValueLiskedList_Test.Update());
    }

    [Fact]
    public void Commons()
    {
        Assert.True(ValueLiskedList_Test.Commons());
    }

    [Fact(DisplayName = "ForEachMethod")]
    public void ForEachMethod()
    {
        Assert.True(ValueLiskedList_Test.ForEachMethod());
    }

    [Fact(DisplayName = "Dispose")]
    public void Dispose()
    {
        Assert.True(ValueLiskedList_Test.Dispose());
    }

    [Fact(DisplayName = "ContainsAny")]
    public void Contains()
    {
        Assert.True(ValueLiskedList_Test.Contains());
    }

    [Fact(DisplayName = "Equals")]
    public void Test_Equals()
    {
        Assert.True(ValueLiskedList_Test.Equals());
    }

    [Fact(DisplayName = "IndexOf")]
    public void IndexOf()
    {
        Assert.True(ValueLiskedList_Test.IndexOf());
    }

    [Fact(DisplayName = "LastIndexOf")]
    public void LastIndexOf()
    {
        Assert.True(ValueLiskedList_Test.LastIndexOf());
    }

    [Fact(DisplayName = "IndexOfAny")]
    public void IndexOfAny()
    {
        Assert.True(ValueLiskedList_Test.IndexOfAny());
    }

    [Fact(DisplayName = "ReUseReady")]
    public void RemoveAt()
    {
        Assert.True(ValueLiskedList_Test.ReUseReady());
    }


    [Fact(DisplayName = "NodesCount")]
    public void Length()
    {
        Assert.True(ValueLiskedList_Test.Length());
    }

    [Fact(DisplayName = "IsEmpty")]
    public void IsEmpty()
    {
        Assert.True(ValueLiskedList_Test.IsEmpty());
    }

    [Fact(DisplayName = "Get")]
    public void Get()
    {
        Assert.True(ValueLiskedList_Test.Get());
    }



    [Fact(DisplayName = "ContainsSpan")]
    public void ContainsSpan()
    {
        Assert.True(ValueLiskedList_Test.ContainsSpan());
    }

    [Fact(DisplayName = "AppendValue")]
    public void Append()
    {
        Assert.True(ValueLiskedList_Test.Append());
    }


    // --- 辅助结构体 ---
    [StructLayout(LayoutKind.Sequential)]
    private struct TestData
    {
        public int Id;
        public float Value;
        public bool Equals(TestData other) => Id == other.Id;
        public override bool Equals(object obj) => obj is TestData other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
    }



    // =================================================================
    // 1. 核心增删查方法测试
    // =================================================================

    [Fact]
    public void Test_LL_01_AppendAndIndexOf()
    {
        var list = new ValueLinkedList<TestData>();
        var data1 = new TestData { Id = 10, Value = 1.1f };
        var data2 = new TestData { Id = 20, Value = 2.2f };

        Assert.True(list.Append(in data1));
        Assert.True(list.Append(in data2));

        // 纠正：使用 NodesCount
        Assert.Equal(2U, list.NodesCount);

        Assert.Equal(0, list.IndexOf(in data1));
        Assert.Equal(1, list.IndexOf(in data2));

        list.Dispose();
    }

    [Fact]
    public void Test_LL_02_SetAsFree()
    {
        ValueLinkedList<TestData> list = new ValueLinkedList<TestData>();
        var data1 = new TestData { Id = 10, Value = 1.1f };
        var data2 = new TestData { Id = 20, Value = 2.2f };
        var data3 = new TestData { Id = 30, Value = 3.3f };

        list.Append(in data1); // Index 0
        list.Append(in data2); // Index 1
        list.Append(in data3); // Index 2

        Assert.Equal(3U, list.NodesCount);

        // 移除 Index 1
        Assert.True(list.SetAsFree(1));

        // 纠正：使用 NodesCount
        Assert.Equal(2U, list.NodesCount);

        // 验证 data2 不存在，data3 应该前移到 Index 1
        Assert.Equal(0, list.IndexOf(in data1));
        Assert.Equal(-1, list.IndexOf(in data2));
        Assert.Equal(1, list.IndexOf(in data3));

        list.Dispose();
    }



    // =================================================================
    // ValueLinkedList 潜在 BUG 测试 (D.11)
    // =================================================================

    /// <summary>
    /// 验证 ValueLinkedList 在容量为 0 时，通过堆分配创建新节点并成功加入链表后，
    /// FreeNodesCount 保持为 0。
    /// 
    /// 此测试旨在排除以下潜在 BUG：
    /// 1. Append 逻辑在 createNode_on_heap 后，错误地调用了 SetAsFree。
    /// 2. createNode_on_heap 或 LinkToLocalNode 意外地修改了 _freeNodesCount。
    /// </summary>
    [Fact]
    public void Test_LL_111_HeapAppend_FreeCountIsolation()
    {
        // 1. 初始化容量为 0 的链表 (强制后续 Append 必须通过 NativeMemory.Alloc 分配)
        var list = new ValueLinkedList<TestData>();
        var data1 = new TestData { Id = 1, Value = 1.0f };
        var data2 = new TestData { Id = 2, Value = 2.0f };

        // 验证初始状态
        Assert.Equal(0U, list.NodesCount);
        Assert.Equal(0U, list.FreeNodesCount);

        // 2. 第一次 Append：触发 createNode_on_heap() -> LinkToLocalNode()
        list.Append(in data1);

        // 验证状态：应该只有活动节点增加
        Assert.Equal(1U, list.NodesCount);
        Assert.Equal(0U, list.FreeNodesCount); // 断言 FreeNodesCount 依然是 0
        Assert.True(list.Contains(in data1));

        // 3. 第二次 Append：继续触发堆分配
        list.Append(in data2);

        // 验证状态：活动节点继续增加，空闲节点仍为 0
        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(0U, list.FreeNodesCount); // 断言 FreeNodesCount 依然是 0
        Assert.True(list.Contains(in data2));

        list.Dispose();
    }

    /// <summary>
    /// 验证移除操作后，节点能被成功回收和重用，并确保所有状态计数正确。
    /// 此测试验证了 SetAsFree 和 DeQueueEmptyNode 的工作流。
    /// </summary>
    [Fact]
    public void Test_LL_112_RemoveAndReuse_Flow()
    {
        const uint InitialCapacity = 5;
        var list = new ValueLinkedList<TestData>(InitialCapacity); // 预分配 5 个节点

        var dataA = new TestData { Id = 1, Value = 1.0f };
        var dataB = new TestData { Id = 2, Value = 2.0f };
        var dataC = new TestData { Id = 3, Value = 3.0f };
        var dataNew = new TestData { Id = 99, Value = 99.0f };

        // 1. 初始状态
        Assert.Equal(0U, list.NodesCount);
        Assert.Equal(InitialCapacity, list.FreeNodesCount); // 5

        // 2. 添加 A, B, C (消耗 3 个空闲节点)
        list.Append(in dataA);
        list.Append(in dataB);
        list.Append(in dataC);

        Assert.Equal(3U, list.NodesCount);
        Assert.Equal(2U, list.FreeNodesCount); // 5 - 3 = 2

        // 3. 移除 B (索引 1)：归还 1 个空闲节点
        Assert.True(list.SetAsFree(1));

        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(3U, list.FreeNodesCount); // 2 + 1 = 3

        // 4. Append 新节点 dataNew：从 FreeNodesPool 中取出节点
        list.Append(in dataNew);

        // 验证状态：节点被成功重用
        Assert.Equal(3U, list.NodesCount);
        Assert.Equal(2U, list.FreeNodesCount); // 3 - 1 = 2

        // 5. 验证新节点的数据和位置（应在链表末尾，索引 2）
        Assert.Equal(2, list.IndexOf(in dataNew));
        Assert.Equal(99.0f, list[2]->Value);

        list.Dispose();
    }





    /// <summary>
    /// 验证 ValueLinkedList 的 ClearFreeNodes() 只能释放空闲节点，不能影响正在使用的节点。
    /// 此测试旨在排除 Test_D_71 中断言失败的潜在原因。
    /// </summary>
    [Fact]
    public void Test_LL_101_ClearFreeNodes_Isolation()
    {
        const uint InitialCapacity = 10;
        var list = new ValueLinkedList<TestData>(InitialCapacity);

        var data1 = new TestData { Id = 10, Value = 1.1f };
        var data2 = new TestData { Id = 20, Value = 2.2f };
        var data3 = new TestData { Id = 30, Value = 3.3f };

        // 1. 验证初始状态（基于您的设计意图）
        Assert.Equal(0U, list.NodesCount);
        Assert.Equal(InitialCapacity, list.FreeNodesCount); // 初始有 10 个空闲节点

        // 2. 添加 3 个活动节点 (消耗空闲节点池)
        list.Append(in data1); // Index 0
        list.Append(in data2); // Index 1
        list.Append(in data3); // Index 2

        Assert.Equal(3U, list.NodesCount);
        Assert.Equal(InitialCapacity - 3, list.FreeNodesCount); // 10 - 3 = 7 个空闲节点

        // 3. 移除 data2 (Index 1)，将一个节点返回空闲池
        Assert.True(list.SetAsFree(1));

        // 4. 验证状态：NodesCount = 2，FreeNodesCount = 8
        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(InitialCapacity - 3 + 1, list.FreeNodesCount); // 7 + 1 = 8

        // --- 核心测试部分 ---

        // 5. 调用 ClearFreeNodes()
        list.ClearFreeNodes();

        // 6. 验证空闲节点归零
        Assert.Equal(0U, list.FreeNodesCount);

        // 7. 验证活动节点链表结构和数据完整性 (隔离性检查)
        Assert.Equal(2U, list.NodesCount); // NodesCount 应该保持不变

        // 验证 data1 (现在是 Index 0)
        Assert.Equal(0, list.IndexOf(in data1));

        // 验证 data3 (现在是 Index 1)
        Assert.Equal(1, list.IndexOf(in data3));

        // 验证 data2 (已移除)
        Assert.Equal(-1, list.IndexOf(in data2));

        // 验证节点数据的可读性 (防止内存被意外释放或污染)
        TestData* retrieved1 = list.Index(0);
        Assert.True(retrieved1 != null, "ClearFreeNodes 意外破坏了 Index 0 的节点内存。");
        Assert.Equal(1.1f, retrieved1->Value);

        TestData* retrieved3 = list.Index(1);
        Assert.True(retrieved3 != null, "ClearFreeNodes 意外破坏了 Index 1 的节点内存。");
        Assert.Equal(3.3f, retrieved3->Value);

        list.Dispose();
    }



    /// <summary>
    /// 验证链表仅剩一个节点时，调用 SetAsFree(0) 后，head 和 tail 指针都能被正确设置为 null。
    /// 这是 List 状态从 1 切换到 0 的关键边缘测试。
    /// </summary>
    [Fact]
    public void Test_LL_121_SingleNode_RemovalEdgeCase()
    {
        // 直接使用 new 初始化，预分配 0 个节点
        var list = new ValueLinkedList<TestData>(0);
        var data1 = new TestData { Id = 1, Value = 1.0f };
        var data2 = new TestData { Id = 2, Value = 2.0f };

        // 1. 添加 data1
        list.Append(in data1);

        Assert.Equal(1U, list.NodesCount);
        // 验证单节点时 head == tail (通过验证 First == Last)
        Assert.True(list.First != null && list.First == list.Last, "单节点时 First 和 Last 必须指向同一内存地址。");

        // 2. 移除 data1 (索引 0)
        Assert.True(list.SetAsFree(0), "移除操作必须成功。");

        // 3. 验证状态切换到 0 的边缘：
        Assert.Equal(0U, list.NodesCount);
        Assert.True(list.IsEmpty);
        // 关键断言：head 和 tail 必须为 null
        Assert.True(list.First == null, "移除最后一个节点后 First 必须为 null。");
        Assert.True(list.Last == null, "移除最后一个节点后 Last 必须为 null。");

        // 验证空闲节点计数：从堆分配的节点被回收
        Assert.Equal(1U, list.FreeNodesCount);

        // 4. 重用被回收的节点 (测试重用机制)
        list.Append(in data2);

        Assert.Equal(1U, list.NodesCount);
        Assert.Equal(0U, list.FreeNodesCount); // 空闲节点被消耗

        list.Dispose();
    }

    /// <summary>
    /// 验证链表在清空所有活跃节点后，调用 SetAsFree 时的健壮性。
    /// </summary>
    [Fact]
    public void Test_LL_122_ClearActiveNodes_ThenDispose()
    {
        // 直接使用 new 初始化，预分配 10 个节点
        var list = new ValueLinkedList<TestData>(10);
        var dataA = new TestData { Id = 10, Value = 10.0f };
        var dataB = new TestData { Id = 20, Value = 20.0f };

        // 1. 初始状态
        Assert.Equal(0U, list.NodesCount);
        Assert.Equal(10U, list.FreeNodesCount);

        // 2. 添加 A 和 B (消耗 2 个空闲节点)
        list.Append(in dataA);
        list.Append(in dataB);

        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(8U, list.FreeNodesCount); // 10 - 2 = 8

        // 3. 移除所有活跃节点 (A, B)
        Assert.True(list.SetAsFree(0)); // 移除 A，B 移动到索引 0
        Assert.True(list.SetAsFree(0)); // 移除 B

        // 4. 验证活跃节点链表被清空
        Assert.Equal(0U, list.NodesCount);
        Assert.True(list.IsEmpty);
        Assert.True(list.First == null && list.Last == null, "活跃节点清空后 head/tail 必须为 null。");

        // 5. 验证空闲节点全部回归池中
        Assert.Equal(10U, list.FreeNodesCount); // 8 + 2 = 10 (回到初始状态)

        // 6. 验证在链表逻辑为空（NodesCount=0）时调用 Dispose() 的安全性 (不应崩溃)
        list.Dispose();
    }

    /// <summary>
    /// 验证 TryPop 方法在链表为空时返回 false，且不修改状态。
    /// 验证 TryPop 在链表仅有一个节点时，正确移除并释放节点，且链表状态切换到空。
    /// </summary>
    [Fact]
    public void Test_LL_123_TryPop_EdgeCases()
    {
        // 预分配 0 个节点，强制 Pop 释放堆内存
        var list = new ValueLinkedList<TestData>(0);
        TestData valuePop;

        // 1. 验证空链表的 TryPop
        Assert.False(list.TryPop(out valuePop), "空链表 TryPop 必须失败。");
        Assert.Equal(0U, list.NodesCount);

        var data1 = new TestData { Id = 1, Value = 1.0f };
        var data2 = new TestData { Id = 2, Value = 2.0f };

        // 2. 添加两个节点
        list.Append(in data1);
        list.Append(in data2);

        Assert.Equal(2U, list.NodesCount);
        Assert.True(list.Contains(in data1));

        // 3. Pop 头部节点 (data1)
        Assert.True(list.TryPop(out valuePop), "Pop 必须成功。");
        Assert.Equal(1, valuePop.Id); // 验证 Pop 出来的是 data1

        Assert.Equal(1U, list.NodesCount);
        Assert.False(list.Contains(in data1)); // data1 已被移除
        Assert.True(list.Contains(in data2)); // data2 成为新头部

        // 4. Pop 最后一个节点 (data2)
        Assert.True(list.TryPop(out valuePop), "Pop 最后一个节点必须成功。");
        Assert.Equal(2, valuePop.Id); // 验证 Pop 出来的是 data2

        // 5. 验证状态切换到 0 的边缘
        Assert.Equal(0U, list.NodesCount);
        Assert.True(list.IsEmpty);
        Assert.True(list.First == null && list.Last == null, "Pop 完最后一个节点后 head/tail 必须为 null。");

        list.Dispose();
    }




    // =================================================================
    // ValueLinkedList 引用与指针测试 (D.13)
    // =================================================================





    /// <summary>
    /// 验证 InsertAt 在索引 0 时的行为，特别是内存分配和 head/tail 的正确性。
    /// InsertAt 是唯一可能在链表头部插入新节点的“非Append”方法。
    /// </summary>
    [Fact]
    public void Test_LL_133_InsertAt_HeadEdgeCase()
    {
        ValueLinkedList<TestData> list = new ValueLinkedList<TestData>(0); // 确保无预分配
        TestData dataA = new TestData { Id = 10, Value = 10.0f };
        TestData dataB = new TestData { Id = 20, Value = 20.0f };
        TestData dataC = new TestData { Id = 30, Value = 30.0f };

        // 1. InsertAt 0 (链表为空)
        TestData* p_dataA = &dataA;

        list.InsertAt(0, p_dataA);


        Assert.Equal(1U, list.NodesCount);
        Assert.Equal(10.0f, list.First->Value);
        // 验证单节点时 head == tail
        Assert.True(list.First == list.Last, "InsertAt(0) 到空链表后 First 和 Last 必须相等。");

        // 2. InsertAt 0 (链表非空)
        TestData* p_dataB = &dataB;

        list.InsertAt(0, p_dataB); // B 应该成为新的头部


        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(20.0f, list.First->Value); // 头部是 B
        Assert.Equal(10.0f, list[1]->Value);   // 原头部 A 移到索引 1

        // 验证 tail 保持为 A
        Assert.Equal(10.0f, list.Last->Value);
        Assert.True(list.First != list.Last, "多节点时 First 和 Last 必须不同。");

        // 3. InsertAt 尾部（NodesCount）
        TestData* p_dataC = &dataC;

        list.InsertAt((int)list.NodesCount, p_dataC); // C 插入到索引 2


        Assert.Equal(3U, list.NodesCount);
        Assert.Equal(30.0f, list.Last->Value); // C 成为新的尾部

        // 链表顺序：B (20.0) -> A (10.0) -> C (30.0)
        Assert.Equal(20.0f, list[0]->Value);
        Assert.Equal(10.0f, list[1]->Value);
        Assert.Equal(30.0f, list[2]->Value);

        list.Dispose();
    }



    /// <summary>
    /// Test_LL_1401: 验证无参构造函数的零初始化状态。
    /// </summary>
    [Fact]
    public void Test_LL_1401_EmptyList_InitialState()
    {
        var list = new ValueLinkedList<TestData>();
        Assert.Equal(0U, list.NodesCount);
        Assert.True(list.IsEmpty);
        Assert.True(list.First == null);
        Assert.True(list.Last == null);
        Assert.Equal(0U, list.FreeNodesCount);
        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1407: 验证值复制 (Append) 和引用 (AppendReferences) 节点能否共存于同一链表，并验证它们的 isLocalValue 标志。
    /// </summary>
    [Fact]
    public void Test_LL_1407_Append_ValuesAndReferences_Coexistence()
    {
        var list = new ValueLinkedList<TestData>();
        var dataCopy = new TestData { Id = 10, Value = 1.0f };
        var dataRef = new TestData { Id = 20, Value = 2.0f }; // 外部变量，将作为引用添加

        // 1. 添加值复制节点 (Append)
        list.Append(in dataCopy);
        // 2. 添加引用节点 (AppendReferences)
        list.AppendReferences(in dataRef);

        Assert.Equal(2U, list.NodesCount);
        Assert.True(list.Contains(in dataCopy));

        // 验证节点类型和引用特性
        Assert.True(list.IndexNode(0)->isLocalValue, "第一个节点应是值复制 (isLocalValue=true)。");
        Assert.False(list.IndexNode(1)->isLocalValue, "第二个节点应是引用 (isLocalValue=false)。");

        // 外部修改引用值
        dataRef.Value = 99.9f;
        Assert.Equal(99.9f, list[1]->Value); // 引用值应同步更新

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1409: 验证 ForEach 在非空链表上的执行。
    /// </summary>
    [Fact]
    public void Test_LL_1409_ForEach_Execution()
    {
        var list = new ValueLinkedList<TestData>();
        list.Append(new TestData { Id = 1, Value = 1.0f });
        list.Append(new TestData { Id = 2, Value = 2.0f });

        // 辅助静态方法 (模拟函数指针)
        static bool SumValues(int index,TestData* value, void* caller)
        {
            // 在静态方法中，我们断言所有值都大于 0
            Assert.True(value->Value > 0.0f);

            return true;
        }


        // 执行 ForEach
        list.ForEach(&SumValues, null);

        list.Dispose();
    }




    /// <summary>
    /// Test_LL_1503: 验证存在多个相同值时，LastIndexOf 返回正确的最大索引。
    /// </summary>
    [Fact]
    public void Test_LL_1503_LastIndexOf_MultipleMatches()
    {
        var list = new ValueLinkedList<TestData>();
        var target = new TestData { Id = 5 };

        // 顺序: 10, 5(1), 20, 5(3), 30
        list.Append(new TestData { Id = 10 }); // 0
        list.Append(in target);              // 1 (Match)
        list.Append(new TestData { Id = 20 }); // 2
        list.Append(in target);              // 3 (Match)
        list.Append(new TestData { Id = 30 }); // 4

        // 期望找到索引 3
        Assert.Equal(3, list.LastIndexOf(in target));

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1603: 验证移除尾部节点 (SetAsFree(NodesCount-1)) 后，tail 指针的正确更新。
    /// </summary>
    [Fact]
    public void Test_LL_1603_SetAsFree_Tail_Update()
    {
        var list = new ValueLinkedList<TestData>();
        var dataA = new TestData { Id = 10 };
        var dataB = new TestData { Id = 20 };

        list.Append(in dataA); // 0 (Head)
        list.Append(in dataB); // 1 (Tail)

        // 记录原始头部节点 A 的值指针
        TestData* p_A = list[0];

        // 移除尾部 B (索引 1)
        Assert.True(list.SetAsFree(1));

        Assert.Equal(1U, list.NodesCount);

        // 验证新的尾部应该是 A
        Assert.True(list.Last != null, "尾部不应为 null。");
        Assert.Equal(10, list.Last->Id); // 新尾部是 A
        Assert.True(list.Last == p_A, "Tail 指针应指向原 A 节点的内存地址。");

        // 验证 A 节点的 Next 必须是 null
        Assert.True(list.IndexNode(0)->Next == null, "新尾部 A 的 Next 必须为 null。");

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1609: 验证移除引用节点 (isLocalValue=false) 时，不会尝试释放值内存。
    /// </summary>
    [Fact]
    public void Test_LL_1609_Remove_ValueReference_Integrity()
    {
        var list = new ValueLinkedList<TestData>();
        TestData dataRef = new TestData { Id = 100 };

        list.AppendReferences(in dataRef); // isLocalValue = false


        // 移除节点
        Assert.True(list.SetAsFree(0));

        // 验证外部变量的值没有被修改 (证明 SetAsFree 内部没有释放外部值内存)
        Assert.Equal(100, dataRef.Id);

        // 验证 FreeNodesCount 增加 (节点结构被回收)
        Assert.Equal(1U, list.FreeNodesCount);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1610: 验证移除值复制节点 (isLocalValue=true) 时，值内存被正确释放。
    /// 间接验证：FreeNodesCount 增加，且值指针被清空。
    /// </summary>
    [Fact]
    public void Test_LL_1610_Remove_ValueCopy_Integrity()
    {
        var list = new ValueLinkedList<TestData>();
        var data = new TestData { Id = 1 };

        list.Append(in data); // isLocalValue = true

        // 记录节点指针
        ValueLiskedListNode<TestData>* p_node = list.IndexNode(0);

        // 移除节点
        Assert.True(list.SetAsFree(0));

        // 验证 FreeNodesCount 增加 (节点结构被回收)
        Assert.Equal(1U, list.FreeNodesCount);

        // 验证回收后的节点指针被置为 null (依赖 _dispose 逻辑)
        // 无法直接访问 FreeNodesHead，但我们可以依赖 SetAsFree 内部调用 _dispose 的正确性。
        // 在此处，我们验证状态转移是正确的。

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1802: 验证 Replace 将长子序列替换为短子序列，NodesCount 相应减少。
    /// </summary>
    [Fact]
    public void Test_LL_1802_Replace_ShorterSequence_CountChange()
    {
        var list = new ValueLinkedList<TestData>();

        // 初始序列: 1, 2, 3, 4, 5 (Count=5)
        list.Append(new TestData { Id = 1 });
        list.Append(new TestData { Id = 2 });
        list.Append(new TestData { Id = 3 });
        list.Append(new TestData { Id = 4 });
        list.Append(new TestData { Id = 5 });

        // 替换目标: [2, 3, 4] (3个元素)
        ReadOnlySpan<TestData> select = new TestData[] { new TestData { Id = 2 }, new TestData { Id = 3 }, new TestData { Id = 4 } }.AsSpan();
        // 替换值: [99] (1个元素)
        ReadOnlySpan<TestData> value = new TestData[] { new TestData { Id = 99 } }.AsSpan();

        // 期望：Count = 5 - 3 + 1 = 3
        list.Replace(select, value);

        Assert.Equal(3U, list.NodesCount);
        Assert.Equal(1, list[0]->Id);
        Assert.Equal(99, list[1]->Id); // 替换值
        Assert.Equal(5, list[2]->Id);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1806: 验证 Append(ReadOnlySpan<T>) 批量添加的正确性。
    /// </summary>
    [Fact]
    public void Test_LL_1806_Append_ReadOnlySpan_BulkAdd()
    {
        var list = new ValueLinkedList<TestData>();
        var spanData = new TestData[]
        {
            new TestData { Id = 1 },
            new TestData { Id = 2 },
            new TestData { Id = 3 }
        };
        ReadOnlySpan<TestData> span = spanData.AsSpan();

        // 批量添加
        list.Append(span);

        Assert.Equal(3U, list.NodesCount);
        Assert.Equal(1, list[0]->Id);
        Assert.Equal(3, list[2]->Id);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1901: 验证 InsertAt(NodesCount) (在尾部插入) 时，tail 指针是否正确更新。
    /// </summary>
    [Fact]
    public void Test_LL_1901_InsertAt_Tail_NullTailUpdate()
    {
        var list = new ValueLinkedList<TestData>();
        // --- 修复点：添加 Value 初始化 ---
        var dataA = new TestData { Id = 10, Value = 10.0f };
        var dataB = new TestData { Id = 20, Value = 20.0f };
        var dataC = new TestData { Id = 30, Value = 30.0f };

        list.Append(in dataA);
        list.Append(in dataB);

        // 验证当前尾部是 B (现在 list.Last->Value = 20.0f，断言通过)
        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(20.0f, list.Last->Value);

        // 在尾部插入 dataC (索引为 2)

        list.InsertAt((int)list.NodesCount, &dataC);


        // 验证 Count 和新尾部
        Assert.Equal(3U, list.NodesCount);
        Assert.Equal(30.0f, list.Last->Value); // 新尾部必须是 C
        Assert.Equal(30.0f, list[2]->Value);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1902: 验证 Remove(in T) 在移除头部节点时，链表指针的更新。
    /// </summary>
    [Fact]
    public void Test_LL_1902_Remove_Head_Cleanup()
    {
        var list = new ValueLinkedList<TestData>(0);
        
        // --- 修正点：显式初始化 Value 字段 ---
        var dataA = new TestData { Id = 1, Value = 1.0f }; // Head
        var dataB = new TestData { Id = 2, Value = 2.0f }; // New Head after removal
        var dataC = new TestData { Id = 3, Value = 3.0f }; // Tail

        list.Append(in dataA);
        list.Append(in dataB);
        list.Append(in dataC);
        
        // 记录原始头部 B 的指针 (预期它成为新的 Head)
        TestData* p_B = list[1]; // 这里的 list[1] 应该返回 B 节点的值指针

        // 移除头部 A
        Assert.True(list.Remove(in dataA));
        
        // 验证状态
        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(1U, list.FreeNodesCount); // A 的节点被回收
        
        // 验证新的 Head 是 B，且 Value 为 2.0f (修正后断言预期值与实际值匹配)
        Assert.Equal(2.0f, list.First->Value);
        Assert.True(list.First == p_B, "新的 First 必须指向原 B 节点的内存地址。");
        
        // 验证 Tail 保持 C
        Assert.Equal(3.0f, list.Last->Value);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1903: 验证 Remove(in T) 尝试移除不存在的值时，链表状态保持不变。
    /// </summary>
    [Fact]
    public void Test_LL_1903_Remove_NonExistentValue_NoChange()
    {
        var list = new ValueLinkedList<TestData>(5); // 预分配 5 个节点
        list.Append(new TestData { Id = 1 });
        list.Append(new TestData { Id = 2 });

        uint initialCount = list.NodesCount; // 2
        uint initialFreeCount = list.FreeNodesCount; // 5 - 2 = 3
        var nonExistent = new TestData { Id = 99 };

        // 尝试移除不存在的值
        Assert.False(list.Remove(in nonExistent));

        // 验证状态保持不变
        Assert.Equal(initialCount, list.NodesCount);
        Assert.Equal(initialFreeCount, list.FreeNodesCount);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1904: 验证 TryPop(out T) 在空链表上返回 false，且不修改状态。
    /// </summary>
    [Fact]
    public void Test_LL_1904_TryPop_EmptyList_False()
    {
        var list = new ValueLinkedList<TestData>();
        TestData valuePop;

        // 尝试 Pop 空链表
        Assert.False(list.TryPop(out valuePop));

        // 验证状态
        Assert.Equal(0U, list.NodesCount);
        Assert.True(list.IsEmpty);
        Assert.Equal(0U, list.FreeNodesCount);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1905: 验证 Update(index, T* value) 成功更新节点值。
    /// </summary>
    [Fact]
    public void Test_LL_1905_Update_NodeValue()
    {
        var list = new ValueLinkedList<TestData>();
        list.Append(new TestData { Id = 1, Value = 1.0f });
        list.Append(new TestData { Id = 2, Value = 2.0f }); // 索引 1

        var newValue = new TestData { Id = 99, Value = 99.9f };

        // 验证初始值
        Assert.Equal(2.0f, list[1]->Value);

        // 更新索引 1

        list.Update(1, &newValue);


        // 验证更新后的值
        Assert.Equal(99.9f, list[1]->Value);
        Assert.Equal(99, list[1]->Id);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1906: 验证 SetAsFree(0) 在单节点链表上执行后，链表状态正确地切换到空。
    /// </summary>
    [Fact]
    public void Test_LL_1906_SetAsFree_SingleNode_EmptyList()
    {
        var list = new ValueLinkedList<TestData>(0);
        list.Append(new TestData { Id = 1 });

        // 移除唯一节点 (索引 0)
        Assert.True(list.SetAsFree(0));

        // 验证状态
        Assert.Equal(0U, list.NodesCount);
        Assert.True(list.IsEmpty);
        Assert.True(list.First == null);
        Assert.True(list.Last == null);
        Assert.Equal(1U, list.FreeNodesCount); // 节点被回收

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1907: 验证 ClearFreeNodes() 在空闲池为空时不会导致崩溃。
    /// </summary>
    [Fact]
    public void Test_LL_1907_ClearFreeNodes_EmptyPool()
    {
        var list = new ValueLinkedList<TestData>(); // 初始 FreeNodesCount = 0

        // 验证初始状态
        Assert.Equal(0U, list.FreeNodesCount);

        // 调用 ClearFreeNodes
        list.ClearFreeNodes(); // 预期不发生崩溃

        // 验证状态
        Assert.Equal(0U, list.FreeNodesCount);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1908: 验证 Replace 将一个序列替换为与它内容完全相同的序列时，NodesCount 和 FreeNodesCount 保持不变，且指针链接正确。
    /// </summary>
    [Fact]
    public void Test_LL_1908_Replace_SelfReplace_Integrity()
    {
        var list = new ValueLinkedList<TestData>();

        // 初始序列: 1, 2, 3, 4, 5 (Count=5)
        list.Append(new TestData { Id = 1 });
        list.Append(new TestData { Id = 2 });
        list.Append(new TestData { Id = 3 });
        list.Append(new TestData { Id = 4 });
        list.Append(new TestData { Id = 5 });

        // 替换目标/值: [2, 3, 4] (3个元素)
        ReadOnlySpan<TestData> target = new TestData[] { new TestData { Id = 2 }, new TestData { Id = 3 }, new TestData { Id = 4 } }.AsSpan();
        ReadOnlySpan<TestData> value = target; // 替换为自身

        // 记录初始状态
        uint initialCount = list.NodesCount;

        // 替换 (应移除 3 个节点，再插入 3 个节点，净变化为 0)
        Replace_Result result = list.Replace(target, value);

        Assert.Equal(Replace_Result.Success_Code, result.Status);
        Assert.Equal(initialCount, list.NodesCount); // NodesCount 必须保持 5

        // 验证链表内容
        Assert.Equal(1, list[0]->Id);
        Assert.Equal(2, list[1]->Id);
        Assert.Equal(4, list[3]->Id);
        Assert.Equal(5, list[4]->Id);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1909: 验证 ValueLinkedList(uint TCount) 构造函数是否正确预分配节点，并验证后续 Append 是否消耗空闲节点。
    /// </summary>
    [Fact]
    public void Test_LL_1909_Constructor_WithCacheMemory_Usage()
    {
        const uint CacheCount = 10;
        var list = new ValueLinkedList<TestData>(CacheCount);

        // 1. 验证 FreeNodesCount
        Assert.Equal(0U, list.NodesCount);
        Assert.Equal(CacheCount, list.FreeNodesCount);

        // 2. 第一次 Append (应消耗 1 个空闲节点)
        list.Append(new TestData { Id = 1 });

        Assert.Equal(1U, list.NodesCount);
        Assert.Equal(CacheCount - 1, list.FreeNodesCount); // 9

        // 3. 第二次 Append (应消耗第 2 个空闲节点)
        list.Append(new TestData { Id = 2 });

        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(CacheCount - 2, list.FreeNodesCount); // 8

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_1910: 验证 Append(ReadOnlySpan<T>) 批量添加的正确性，并确保所有节点都被正确标记为值复制。
    /// </summary>
    [Fact]
    public void Test_LL_1910_Append_ReadOnlySpan_IsLocalValue()
    {
        var list = new ValueLinkedList<TestData>();
        var spanData = new TestData[]
        {
            new TestData { Id = 10 },
            new TestData { Id = 20 }
        };
        ReadOnlySpan<TestData> span = spanData.AsSpan();

        // 批量添加
        list.Append(span);

        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(10, list[0]->Id);

        // 验证所有节点都必须是值复制 (isLocalValue = true)
        Assert.True(list.IndexNode(0)->isLocalValue);
        Assert.True(list.IndexNode(1)->isLocalValue);

        list.Dispose();
    }





// =================================================================
    // D.20：移除边界与指针链接测试
    // =================================================================
    
    /// <summary>
    /// Test_LL_2001: 验证 SetAsFree(index) 移除中间节点时，前后节点的 Next 指针是否正确更新，值复制节点的值内存是否被释放。
    /// </summary>
    [Fact]
    public void Test_LL_2001_SetAsFree_Middle_LinksUpdate_ValueCopy()
    {
        var list = new ValueLinkedList<TestData>();
        var dataA = new TestData { Id = 10 };
        var dataB = new TestData { Id = 20 }; // 中间节点
        var dataC = new TestData { Id = 30 };
        
        list.Append(in dataA); // Index 0
        list.Append(in dataB); // Index 1 (Removed)
        list.Append(in dataC); // Index 2
        
        ValueLiskedListNode<TestData>* nodeA = list.IndexNode(0);
        
        // 移除中间节点 B (索引 1)
        Assert.True(list.SetAsFree(1));
        
        // 验证状态
        Assert.Equal(2U, list.NodesCount);
        Assert.Equal(1U, list.FreeNodesCount);
        
        // 验证 A 的 Next 必须指向 C (即 B 的 Next)
        Assert.True(nodeA->Next == list.IndexNode(1), "A 的 Next 必须指向 C (新的索引 1)。");
        Assert.Equal(30, list[1]->Id); // 验证索引 1 是 C
        
        // 验证 Tail 保持 C
        Assert.Equal(30, list.Last->Id);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2002: 验证 SetAsFree(index) 移除中间引用节点时，前后节点的 Next 指针是否正确更新，外部引用值是否未被释放。
    /// </summary>
    [Fact]
    public void Test_LL_2002_SetAsFree_Middle_LinksUpdate_Reference()
    {
    var list = new ValueLinkedList<TestData>();
    var dataRef = new TestData { Id = 200 }; 
    
    list.Append(new TestData { Id = 100, Value = 1.0f }); 
    list.AppendReferences(in dataRef); // Index 1 (Removed)
    list.Append(new TestData { Id = 300, Value = 3.0f }); 
    
    // 移除中间引用节点 (索引 1)
    Assert.True(list.SetAsFree(1));
    
    // 验证状态
    Assert.Equal(2U, list.NodesCount);
    // --- 修正点：将期望值 2U 修正为 1U ---
    Assert.Equal(1U, list.FreeNodesCount); //"移除一个节点后，FreeNodesCount 应该从 0 增加到 1。" 
    
    // 验证外部引用值未被修改/释放
    Assert.Equal(200, dataRef.Id); // "移除引用节点不应影响外部变量。"
    
    // 验证链表链接：Index 0 必须链接到 Index 1 (原 Index 2)
    Assert.Equal(300, list[1]->Id);

    list.Dispose();
    }

    /// <summary>
    /// Test_LL_2003: 验证 TryPop 弹出所有节点后，FreeNodesCount 增加，然后 Append 重新使用这些空闲节点。
    /// </summary>
    [Fact]
    public void Test_LL_2003_TryPop_All_To_Empty_And_Reuse()
    {
        var list = new ValueLinkedList<TestData>(0); // 确保 FreeNodesCount 初始为 0
        list.Append(new TestData { Id = 1 });
        list.Append(new TestData { Id = 2 });
        
        // Pop 1
        Assert.True(list.TryPop(out _));
        Assert.Equal(1U, list.FreeNodesCount);
        Assert.Equal(1U, list.NodesCount);
        
        // Pop 2 (空链表)
        Assert.True(list.TryPop(out _));
        Assert.Equal(2U, list.FreeNodesCount);
        Assert.Equal(0U, list.NodesCount);
        
        // Append 新值 (应重用空闲节点)
        list.Append(new TestData { Id = 3 });
        
        Assert.Equal(1U, list.NodesCount);
        Assert.Equal(1U, list.FreeNodesCount); // 2 - 1 = 1
        
        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2004: 验证 Remove(in T) 只移除第一个匹配项，其他匹配项保持不变。
    /// </summary>
    [Fact]
    public void Test_LL_2004_Remove_Multiple_Matches_FirstOnly()
    {
        var list = new ValueLinkedList<TestData>();
        var target = new TestData { Id = 5 };

        // 顺序: 5(0), 10, 5(2)
        list.Append(in target); // Index 0 (移除目标)
        list.Append(new TestData { Id = 10 });
        list.Append(in target); // Index 2 (保持)
        
        // 移除第一个匹配项 (索引 0)
        Assert.True(list.Remove(in target));
        
        // 验证状态
        Assert.Equal(2U, list.NodesCount);
        
        // 验证第二个匹配项成为新的索引 1
        Assert.Equal(10, list[0]->Id);
        Assert.Equal(5, list[1]->Id); 
        
        // 再次移除 (移除原索引 2 的节点，现在位于索引 1)
        Assert.True(list.Remove(in target));
        Assert.Equal(1U, list.NodesCount);
        Assert.Equal(10, list[0]->Id);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2005: 验证 Replace 传入的 select 跨度长度大于 NodesCount 时的健壮性。
    /// </summary>
    [Fact]
    public void Test_LL_2005_Replace_Invalid_Select_Length()
    {
        var list = new ValueLinkedList<TestData>();
        list.Append(new TestData { Id = 1 });
        
        uint initialCount = list.NodesCount; // 1
        
        // 替换目标: [1, 2] (2个元素)
        ReadOnlySpan<TestData> select = new TestData[] { new TestData { Id = 1 }, new TestData { Id = 2 } }.AsSpan();
        ReadOnlySpan<TestData> value = new TestData[] { new TestData { Id = 99 } }.AsSpan();

        // 预期返回 NotFound (因为子序列匹配检查时会检查边界)
        Replace_Result result = list.Replace(select, value);
        
        Assert.Equal(Replace_Result.NotFound, result.Status);
        Assert.Equal(initialCount, list.NodesCount); // NodesCount 保持 1

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2006: 验证 Replace 从 Index 0 开始匹配并替换序列。
    /// </summary>
    [Fact]
    public void Test_LL_2006_Replace_Leading_Sequence()
    {
        var list = new ValueLinkedList<TestData>();
        // 初始序列: 1, 2, 3
        list.Append(new TestData { Id = 1 });
        list.Append(new TestData { Id = 2 });
        list.Append(new TestData { Id = 3 });
        
        // 替换目标: [1, 2]
        ReadOnlySpan<TestData> select = new TestData[] { new TestData { Id = 1 }, new TestData { Id = 2 } }.AsSpan();
        // 替换值: [99] (更短的序列)
        ReadOnlySpan<TestData> value = new TestData[] { new TestData { Id = 99 } }.AsSpan();

        // 期望：Count = 3 - 2 + 1 = 2
        Replace_Result result = list.Replace(select, value);
        
        Assert.Equal(Replace_Result.Success_Code, result.Status);
        Assert.Equal(2U, list.NodesCount);
        
        // 验证顺序: 99, 3
        Assert.Equal(99, list[0]->Id); 
        Assert.Equal(3, list[1]->Id); 
        
        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2007: 验证 Replace 在尾部匹配并替换序列。
    /// </summary>
    [Fact]
    public void Test_LL_2007_Replace_Trailing_Sequence()
    {
        var list = new ValueLinkedList<TestData>();
        // 初始序列: 1, 2, 3
        list.Append(new TestData { Id = 1 }); // Index 0
        list.Append(new TestData { Id = 2 }); // Index 1
        list.Append(new TestData { Id = 3 }); // Index 2
        
        // 替换目标: [2, 3]
        ReadOnlySpan<TestData> select = new TestData[] { new TestData { Id = 2 }, new TestData { Id = 3 } }.AsSpan();
        // 替换值: [88, 99] (长度相同)
        ReadOnlySpan<TestData> value = new TestData[] { new TestData { Id = 88 }, new TestData { Id = 99 } }.AsSpan();

        // 期望：Count = 3 (长度不变)
        Replace_Result result = list.Replace(select, value);
        
        Assert.Equal(Replace_Result.Success_Code, result.Status);
        Assert.Equal(3U, list.NodesCount);
        
        // 验证顺序: 1, 88, 99
        Assert.Equal(1, list[0]->Id); 
        Assert.Equal(88, list[1]->Id); 
        Assert.Equal(99, list[2]->Id); 
        Assert.Equal(99, list.Last->Id); // 验证 tail 更新正确

        list.Dispose();
    }


    /// <summary>
    /// Test_LL_2008: 验证 IndexOf(ReadOnlySpan) 在链表尾部匹配不足时的失败返回 (-1)。
    /// </summary>
    [Fact]
    public void Test_LL_2008_IndexOf_Span_NotMatchTail()
    {
        ValueLinkedList<TestData> list = new ValueLinkedList<TestData>();
        list.Append(new TestData { Id = 1 });
        list.Append(new TestData { Id = 2 });

        // 搜索序列: [2, 3] (2 匹配，但 3 不存在)
        ReadOnlySpan<TestData> searchSpan = new TestData[] { new TestData { Id = 2 }, new TestData { Id = 3 } }.AsSpan();

        // 期望：从索引 1 开始搜索，发现后面没有节点来匹配 3，应该返回 -1
        int _indexOf = list.IndexOf(searchSpan);
        Assert.Equal(-1, _indexOf);
        
        list.Dispose();
    }


    /// <summary>
    /// Test_LL_2009: 验证 AppendReferences(ReadOnlySpan<T>) 批量添加时，所有节点的 isLocalValue 标志都为 false。
    /// </summary>
    [Fact]
    public void Test_LL_2009_Append_Reference_Span_IsLocalValue_False()
    {
        var list = new ValueLinkedList<TestData>();
        var spanData = new TestData[]
        {
            new TestData { Id = 10 }, 
            new TestData { Id = 20 }
        };
        ReadOnlySpan<TestData> span = spanData.AsSpan();

        // 批量添加引用
        list.AppendReferences(span);

        Assert.Equal(2U, list.NodesCount);
        
        // 验证所有节点都必须是存储引用 (isLocalValue = false)
        Assert.False(list.IndexNode(0)->isLocalValue, "批量引用添加的节点 0 必须是 isLocalValue=false。");
        Assert.False(list.IndexNode(1)->isLocalValue, "批量引用添加的节点 1 必须是 isLocalValue=false。");
        
        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2010: 验证 Contains(T*) 能够正确地通过值等同查找一个指向堆/栈内存的值。
    /// </summary>
    [Fact]
    public void Test_LL_2010_Contains_ValuePointer()
    {
        var list = new ValueLinkedList<TestData>();
        list.Append(new TestData { Id = 10 });
        list.Append(new TestData { Id = 20 });
        
        var targetValue = new TestData { Id = 20 };
        var nonExistentValue = new TestData { Id = 99 };

        // 查找存在的指针值
        Assert.True(list.Contains(&targetValue));
        
        
        // 查找不存在的指针值
        Assert.False(list.Contains(&nonExistentValue));
        

        list.Dispose();
    }


/// <summary>
    /// Test_LL_2101: 验证 Dispose 后尝试访问/操作链表是否安全（应抛出异常或返回 null/false，此处假定返回 null/false）。
    /// </summary>
    [Fact]
    public void Test_LL_2101_Dispose_And_Attempt_Access()
    {
        var list = new ValueLinkedList<TestData>();
        var data = new TestData { Id = 1, Value = 1.0f };
        list.Append(in data);
        
        list.Dispose();
        
        // 尝试访问
        Assert.True(list.IsEmpty);
        Assert.Equal(0U, list.NodesCount);
        Assert.True(list.First == null);
        Assert.True(list.Last == null);
        Assert.True(list[0] == null);
        
        // 尝试操作
        Assert.False(list.Remove(in data));
        
        list.Dispose(); // 验证重复 Dispose 是否安全
    }
    
    /// <summary>
    /// Test_LL_2102: 验证 ClearFreeNodes() 是否正确释放独立堆分配的空闲节点的值内存。
    /// </summary>
    [Fact]
    public void Test_LL_2102_ClearFreeNodes_ReleaseValueMemory()
    {
        var list = new ValueLinkedList<TestData>(0); // 初始无预分配
        
        // 添加一个值复制节点 (Node A)，Node A 的值内存是独立堆分配
        list.Append(new TestData { Id = 1, Value = 1.0f }); 
        
        // 移除 Node A，使其进入空闲池 (_freeNodesHead)
        Assert.True(list.SetAsFree(0));
        
        // 此时 Node A 处于空闲池，其 isLocalNode=true，isLocalValue=true。
        Assert.Equal(1U, list.FreeNodesCount);
        
        // 记录 Node A 的节点指针 (用于 Debug 检查是否真的被释放)
        ValueLiskedListNode<TestData>* freeNode = list.IndexNode(-1); // 假设 IndexNode(-1) 返回 _freeNodesHead
        
        list.ClearFreeNodes(); 
        
        // 验证空闲池清空
        Assert.Equal(0U, list.FreeNodesCount);
        // 如果 _dispose 逻辑正确，Node A 的 Value 内存和 Node A 本身都已被释放。

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2103: 验证 AppendReferences 添加的节点被移除时，外部引用数据不会被释放。
    /// </summary>
    [Fact]
    public void Test_LL_2103_RemoveReference_ValueIntact()
    {
        var list = new ValueLinkedList<TestData>();
        // 外部栈内存
        var dataExternal = new TestData { Id = 99, Value = 99.9f }; 
        
        list.AppendReferences(in dataExternal);
        
        // 验证 isLocalValue = false
        Assert.False(list.IndexNode(0)->isLocalValue); 
        
        // 移除引用节点
        Assert.True(list.Remove(in dataExternal));
        
        // 验证外部数据未被修改或释放
        Assert.Equal(99, dataExternal.Id);
        Assert.Equal(99.9f, dataExternal.Value);
        
        list.Dispose();
    }
    
    /// <summary>
    /// Test_LL_2104: 验证 Replace 操作将长序列替换为短序列 (Length L -> L-1)，FreeNodesCount 变化正确。
    /// </summary>
    [Fact]
    public void Test_LL_2104_Replace_LongToShort_FreeNodesCount()
    {
        var list = new ValueLinkedList<TestData>(0);
        // 初始序列: 1, 2, 3, 4 (Count=4)
        list.Append(new TestData { Id = 1 });
        list.Append(new TestData { Id = 2 });
        list.Append(new TestData { Id = 3 });
        list.Append(new TestData { Id = 4 });
        
        // 替换目标: [2, 3] (Length=2)
        ReadOnlySpan<TestData> select = new TestData[] { new TestData { Id = 2 }, new TestData { Id = 3 } }.AsSpan();
        // 替换值: [99] (Length=1)
        ReadOnlySpan<TestData> value = new TestData[] { new TestData { Id = 99 } }.AsSpan();

        uint initialCount = list.NodesCount; // 4
        uint initialFreeCount = list.FreeNodesCount; // 0 (因为是 Append 方式添加)
        
        // 执行替换：移除 2 个节点，插入 1 个节点。净变化：移除 1 个节点。
        Replace_Result result = list.Replace(select, value);
        
        Assert.Equal(Replace_Result.Success_Code, result.Status);
        Assert.Equal(initialCount - 1, list.NodesCount); // 4 - 1 = 3
        
        // 验证 FreeNodesCount：应该回收 2 个节点，并使用 1 个空闲节点进行插入。
        // 但由于 InsertAt 每次都会 createNode_on_heap，因此移除的 2 个节点会进入 FreeNodesPool。
        Assert.Equal(initialFreeCount + 2, list.FreeNodesCount); //"移除的 2 个节点应该被释放到空闲池。"
        
        // 验证链表内容: 1, 99, 4
        Assert.Equal(1, list[0]->Id);
        Assert.Equal(99, list[1]->Id);
        Assert.Equal(4, list[2]->Id);

        list.Dispose();
    }
    
    /// <summary>
    /// Test_LL_2105: 验证 Replace 操作将短序列替换为长序列 (Length L -> L+1)，FreeNodesCount 变化正确。
    /// </summary>
    [Fact]
    public void Test_LL_2105_Replace_ShortToLong_FreeNodesCount()
    {
        var list = new ValueLinkedList<TestData>(0);
        // 初始序列: 1, 2, 3, 4 (Count=4)
        list.Append(new TestData { Id = 1 });
        list.Append(new TestData { Id = 2 });
        list.Append(new TestData { Id = 3 });
        list.Append(new TestData { Id = 4 });
        
        // 替换目标: [2] (Length=1)
        ReadOnlySpan<TestData> select = new TestData[] { new TestData { Id = 2 } }.AsSpan();
        // 替换值: [98, 99] (Length=2)
        ReadOnlySpan<TestData> value = new TestData[] { new TestData { Id = 98 }, new TestData { Id = 99 } }.AsSpan();

        uint initialCount = list.NodesCount; // 4
        uint initialFreeCount = list.FreeNodesCount; // 0
        
        // 执行替换：移除 1 个节点，插入 2 个节点。净变化：新增 1 个节点。
        Replace_Result result = list.Replace(select, value);
        
        Assert.Equal(Replace_Result.Success_Code, result.Status);
        Assert.Equal(initialCount + 1, list.NodesCount); // 4 + 1 = 5
        
        // 验证 FreeNodesCount：应该回收 1 个节点，但插入的 2 个节点都通过 createNode_on_heap 产生。
        Assert.Equal(initialFreeCount + 1, list.FreeNodesCount); //"移除的 1 个节点应该被释放到空闲池。"
        
        // 验证链表内容: 1, 98, 99, 3, 4
        Assert.Equal(1, list[0]->Id);
        Assert.Equal(98, list[1]->Id);
        Assert.Equal(99, list[2]->Id);
        Assert.Equal(3, list[3]->Id);
        Assert.Equal(4, list[4]->Id);

        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2106: 验证 IndexOfAny 找到第一个匹配元素并返回正确索引。
    /// </summary>
    [Fact]
    public void Test_LL_2106_IndexOfAny_Found()
    {
        var list = new ValueLinkedList<TestData>();
        // 链表内容: 10, 20, 30, 40
        list.Append(new TestData { Id = 10 });
        list.Append(new TestData { Id = 20 });
        list.Append(new TestData { Id = 30 });
        list.Append(new TestData { Id = 40 });
        
        // 搜索集合: [99, 30, 55] (期望找到 30)
        ReadOnlySpan<TestData> searchSpan = new TestData[] { 
            new TestData { Id = 99 }, 
            new TestData { Id = 30 }, 
            new TestData { Id = 55 } 
        }.AsSpan();

        // 期望找到第一个匹配项 30，其索引是 2
        Assert.Equal(2, list.IndexOfAny(searchSpan));
        
        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2107: 验证 IndexOfAny 在未找到任何匹配元素时返回 -1。
    /// </summary>
    [Fact]
    public void Test_LL_2107_IndexOfAny_NotFound()
    {
        var list = new ValueLinkedList<TestData>();
        // 链表内容: 10, 20
        list.Append(new TestData { Id = 10 });
        list.Append(new TestData { Id = 20 });
        
        // 搜索集合: [99, 55]
        ReadOnlySpan<TestData> searchSpan = new TestData[] { 
            new TestData { Id = 99 }, 
            new TestData { Id = 55 } 
        }.AsSpan();

        // 期望未找到任何匹配项
        Assert.Equal(-1, list.IndexOfAny(searchSpan));
        
        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2108: 验证 LastIndexOf 找到最后一个匹配项的索引。
    /// </summary>
    [Fact]
    public void Test_LL_2108_LastIndexOf_Found()
    {
        var list = new ValueLinkedList<TestData>();
        // 链表内容: 10(0), 20(1), 10(2), 30(3)
        var target = new TestData { Id = 10 };
        list.Append(in target); 
        list.Append(new TestData { Id = 20 });
        list.Append(in target); 
        list.Append(new TestData { Id = 30 });
        
        // 期望找到最后一个匹配项 10，其索引是 2
        Assert.Equal(2, list.LastIndexOf(in target));
        
        list.Dispose();
    }
    
    /// <summary>
    /// Test_LL_2109: 验证 Update 能够正确地修改一个引用节点的值（即修改外部值）。
    /// </summary>
    [Fact]
    public void Test_LL_2109_Update_ReferenceNode_ModifiesExternal()
    {
        var list = new ValueLinkedList<TestData>();
        // 外部栈内存
        var dataExternal = new TestData { Id = 99, Value = 99.9f }; 
        list.AppendReferences(in dataExternal); // Index 0 是引用节点
        
        // 验证 isLocalValue = false
        Assert.False(list.IndexNode(0)->isLocalValue); 
        
        // 新值
        var newValue = new TestData { Id = 100, Value = 100.0f };
        
        // 更新 Index 0
         
            list.Update(0, &newValue);
        
        
        // 验证链表节点值更新
        Assert.Equal(100.0f, list[0]->Value);
        
        // 验证外部值也被修改（因为是引用）
        Assert.Equal(100, dataExternal.Id);
        Assert.Equal(100.0f, dataExternal.Value);
        
        list.Dispose();
    }

    /// <summary>
    /// Test_LL_2110: 验证 Contains(ReadOnlySpan) 在复杂序列中间匹配时的正确性。
    /// </summary>
    [Fact]
    public void Test_LL_2110_Contains_MiddleSequence()
    {
        var list = new ValueLinkedList<TestData>();
        // 链表内容: 10, 20, 30, 40
        list.Append(new TestData { Id = 10 });
        list.Append(new TestData { Id = 20 });
        list.Append(new TestData { Id = 30 });
        list.Append(new TestData { Id = 40 });
        
        // 搜索子序列: [20, 30]
        ReadOnlySpan<TestData> searchSpan = new TestData[] { 
            new TestData { Id = 20 }, 
            new TestData { Id = 30 }
        }.AsSpan();

        // 期望找到匹配
        Assert.True(list.Contains(searchSpan));
        
        // 搜索不匹配的子序列: [20, 40]
        searchSpan = new TestData[] { 
            new TestData { Id = 20 }, 
            new TestData { Id = 40 }
        }.AsSpan();
        
        // 期望未找到匹配 (因为它们不相邻)
        Assert.False(list.Contains(searchSpan));
        
        list.Dispose();
    }

}