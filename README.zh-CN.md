# Solamirare

面向 .NET 的零 GC 基础设施 —— 基于 unsafe 内存、无锁数据结构与超低延迟网络，为性能敏感系统而设计。

[English](README.md) | [中文](README.zh-CN.md)
---

## 📦 安装

```bash
dotnet add package Solamirare
```

NuGet: https://www.nuget.org/packages/Solamirare/

---

## ⚡ 为什么选择 Solamirare

- 🚫 **零 GC** —— 无堆分配，无 GC 停顿  
- ⚙️ **完全内存控制** —— 基于 unsafe，精确掌控内存布局与生命周期  
- 🔒 **无锁原语** —— 面向高并发场景设计  
- 🚀 **超低延迟网络** —— IOCP / kqueue / io_uring  
- 🧠 **缓存友好设计** —— 针对现代 CPU 优化  


---

## Benchmark

```
BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]

.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT
 
Json_Performance

| Method                    | Mean         | Error     | StdDev    | Allocated |
|-------------------------- |-------------:|----------:|----------:|----------:|
| CollectionDecode          |     356.2 ns |   0.75 ns |   0.63 ns |         - |
| ObjectStringToDictionary  |     799.2 ns |   7.35 ns |   6.13 ns |         - |
| BaseDecode                | 187,588.4 ns | 520.47 ns | 434.62 ns |         - |
| DocumentToString          |   2,236.7 ns |   7.78 ns |   6.90 ns |         - |
| Objects                   |     600.7 ns |   2.94 ns |   2.75 ns |         - |
| EncodeAndDecodeCollection |   3,253.2 ns |  38.74 ns |  36.23 ns |         - |


MemoryPool_Performance

| Method               | Mean     | Error | Gen0   | Allocated |
|--------------------- |---------:|------:|-------:|----------:|
| BaseMemoryPool_Scale | 3.286 μs |    NA | 0.0153 |      96 B |

ValueDictionary

| Method                   | Mean      | Error    | StdDev   | Allocated |
|------------------------- |----------:|---------:|---------:|----------:|
| Append                   | 506.68 ns | 1.583 ns | 1.480 ns |         - |
| ToJson                   | 728.32 ns | 8.739 ns | 8.174 ns |         - |
| UnManagedString          | 278.07 ns | 2.616 ns | 2.319 ns |         - |
| ForEach                  | 110.27 ns | 0.226 ns | 0.188 ns |         - |
| AddOrUpdate              |  78.42 ns | 0.384 ns | 0.340 ns |         - |
| BasicOperations          |  97.02 ns | 1.356 ns | 1.132 ns |         - |
| CapacityManagement       | 646.51 ns | 3.073 ns | 2.566 ns |         - |
| Collections              |  77.18 ns | 0.247 ns | 0.206 ns |         - |
| IteratorAndRemoveCurrent | 228.00 ns | 4.087 ns | 3.623 ns |         - |
| TryMethods               |  50.41 ns | 0.140 ns | 0.131 ns |         - |
| UtilityMethods           |  56.99 ns | 0.147 ns | 0.122 ns |         - |

UnManagedMemory

| Method                           | Mean         | Error      | StdDev     | Allocated |
|--------------------------------- |-------------:|-----------:|-----------:|----------:|
| Json_EncodeAndDecodeCollection   | 3,298.462 ns | 63.9915 ns | 71.1264 ns |         - |
| Json_Objects                     |   599.492 ns |  1.8521 ns |  1.6418 ns |         - |
| Json_EncodeAndDecodeStrings      | 1,451.016 ns |  7.7575 ns |  6.4779 ns |         - |
| SpiltCopy                        |   197.502 ns |  1.2330 ns |  0.9626 ns |         - |
| SpiltMap                         |    87.893 ns |  0.4164 ns |  0.3251 ns |         - |
| SpiltCopyToValueFrozenDictionary |   449.692 ns |  3.7221 ns |  3.1081 ns |         - |
| SpiltMapToValueFrozenDictionary  |   306.257 ns |  0.9482 ns |  0.7918 ns |         - |
| ReadOnly                         |    24.571 ns |  0.2891 ns |  0.2704 ns |         - |
| CopyTo                           |    33.607 ns |  0.5867 ns |  0.4900 ns |         - |
| ToBytes                          |   108.120 ns |  1.4894 ns |  1.3203 ns |         - |
| Sort                             |   488.807 ns |  1.7119 ns |  1.4295 ns |         - |
| Reverse                          |    58.887 ns |  0.8325 ns |  0.7380 ns |         - |
| Heap_LastIndexOf                 |   102.171 ns |  0.5681 ns |  0.4436 ns |         - |
| EnsureCapacity                   |    11.989 ns |  0.0224 ns |  0.0187 ns |         - |
| Concat                           |    39.524 ns |  0.5928 ns |  0.5255 ns |         - |
| SetValue                         |   109.375 ns |  2.0906 ns |  1.8533 ns |         - |
| IndexOf_Short_Chars              |    37.975 ns |  0.5677 ns |  0.5033 ns |         - |
| IntToUnmanagedString             |    33.535 ns |  0.6917 ns |  1.0972 ns |         - |
| ForEech                          |   148.426 ns |  1.2513 ns |  1.1093 ns |         - |
| Replace                          |   126.133 ns |  2.4266 ns |  2.3832 ns |         - |
| ParseFromDateTime                |   126.847 ns |  1.3960 ns |  1.1657 ns |         - |
| ParseFromLong                    |    74.280 ns |  1.2752 ns |  1.1305 ns |         - |
| ParseFromInt                     |    32.806 ns |  0.3952 ns |  0.3697 ns |         - |
| ParseFromDecimal                 |   121.637 ns |  2.1840 ns |  2.1450 ns |         - |
| IndexOfAny                       | 1,104.630 ns |  9.5223 ns |  8.9072 ns |         - |
| Override_Operate_Equals          |    42.516 ns |  0.5080 ns |  0.4503 ns |         - |
| Contains_Single                  |    37.534 ns |  0.4362 ns |  0.3867 ns |         - |
| ForEachMethod                    |    36.364 ns |  0.5846 ns |  0.5182 ns |         - |
| Resize_Min                       |    58.412 ns |  0.6366 ns |  0.5955 ns |         - |
| Check_on_stack                   |    25.637 ns |  0.1404 ns |  0.1173 ns |         - |
| Contains_Collection              |    42.211 ns |  0.8563 ns |  0.8410 ns |         - |
| Count                            |    53.355 ns |  0.4237 ns |  0.3538 ns |         - |
| Index                            |    43.500 ns |  0.8495 ns |  0.7530 ns |         - |
| RemoveAt                         |    42.813 ns |  0.6719 ns |  0.5957 ns |         - |
| IndexOf_Single_String            |    32.085 ns |  0.5043 ns |  0.4471 ns |         - |
| IndexOf_Single_Char              |    73.524 ns |  1.0368 ns |  0.9698 ns |         - |
| AsRealSizeSpan                   |    32.909 ns |  0.6747 ns |  0.6929 ns |         - |
| AsSpan                           |    28.405 ns |  0.4316 ns |  0.4037 ns |         - |
| IndexOf_Short_Bytes              |   134.399 ns |  1.2851 ns |  1.1392 ns |         - |
| IndexOf_BYTE                     |    57.254 ns |  1.1150 ns |  1.1450 ns |         - |
| Slice                            |    57.736 ns |  1.1559 ns |  1.3312 ns |         - |
| IndexOf_struct                   |   293.499 ns |  2.3115 ns |  1.9302 ns |         - |
| IndexOf_INT                      |   146.008 ns |  2.0242 ns |  1.8934 ns |         - |
| IndexsOf_Chars                   |    93.219 ns |  0.6966 ns |  0.6175 ns |         - |
| InsertAt                         |    67.479 ns |  1.3103 ns |  1.0941 ns |         - |
| InsertCollectionAt               |   172.768 ns |  2.8348 ns |  2.6517 ns |         - |
| RemoveRange                      |    37.050 ns |  0.4698 ns |  0.4395 ns |         - |
| ReSize                           |   226.555 ns |  4.5697 ns | 12.1182 ns |         - |
| GetPointer                       |    31.639 ns |  0.6206 ns |  0.5502 ns |         - |
| AutoIndexMemory                  |    55.814 ns |  0.8467 ns |  0.7071 ns |         - |
| From_Span                        |    45.799 ns |  0.9103 ns |  0.9740 ns |         - |
| From_ExtMemory                   |    29.880 ns |  0.5632 ns |  0.6026 ns |         - |
| Create_Empty                     |     1.576 ns |  0.0439 ns |  0.0367 ns |         - |
| Empty_to_Allocted                |    28.529 ns |  0.5461 ns |  0.5108 ns |         - |
| Reset_From_ExternalMemory        |   199.258 ns |  1.7914 ns |  1.4959 ns |         - |
| Clone                            |    37.332 ns |  0.7872 ns |  1.5721 ns |         - |

ValueLinkedList

| Method       | Mean        | Error      | StdDev     | Median      | Allocated |
|------------- |------------:|-----------:|-----------:|------------:|----------:|
| MixedAppend  | 234.5280 ns |  4.6270 ns |  7.8571 ns | 232.4885 ns |         - |
| Commons      | 328.0527 ns |  5.7511 ns |  9.9204 ns | 326.0876 ns |         - |
| Append       | 421.5139 ns |  8.4454 ns | 23.2612 ns | 417.3453 ns |         - |
| ForEach      | 164.8191 ns |  3.3189 ns |  6.9279 ns | 163.8138 ns |         - |
| ContainsSpan | 361.0976 ns |  3.1316 ns |  2.6151 ns | 361.1179 ns |         - |
| IsEmpty      |   0.3520 ns |  0.0285 ns |  0.0513 ns |   0.3480 ns |         - |
| Get          | 167.8307 ns |  3.3064 ns |  3.8077 ns | 167.4117 ns |         - |
| IndexOfAny   | 255.3806 ns |  5.0127 ns |  4.6889 ns | 256.4516 ns |         - |
| ReUseReady   | 205.1915 ns |  4.0878 ns | 11.6628 ns | 201.3973 ns |         - |
| IndexOf      | 520.0565 ns | 10.2515 ns | 10.9690 ns | 516.0643 ns |         - |
| LastIndexOf  | 330.2075 ns |  6.5032 ns | 10.8655 ns | 327.2139 ns |         - |
| Contains     | 228.5711 ns |  4.2844 ns |  7.2752 ns | 227.0411 ns |         - |
| Dispose      | 168.0699 ns |  3.3833 ns |  6.1866 ns | 167.0798 ns |         - |

```

## 🚀 快速开始

```csharp
unsafe
{
    var memory = new UnManagedMemory<int>(100);

    memory[0] = 42;

    memory.Dispose();
}
```

> ⚠️ 必须在项目中开启 unsafe：

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

---

## 🧩 核心功能

### 🧠 内存与集合

- `UnManagedCollection<T>` —— 基于内存映射的集合结构（支持 Copy / Clone）  
- `UnManagedMemory<T>` —— 可扩展的零 GC 内存结构  
- `UnManagedString` —— 零 GC 字符串抽象  

- [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/UnManagedMemory.cs)
- [高级范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/AdvanceSample_UnManagedMemory.cs)

---

### 📦 数据结构

- `ValueDictionary` —— 零 GC 字典（支持堆模式与映射模式）  [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/ValueDictionary.cs)
- `ValueStack` / `ValueFrozenStack` —— 高性能栈结构  [ValueFrozenStack范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/ValueFrozenStack.cs) | [ValueStack范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/ValueStack.cs)
- `VecDeque` —— 基于环形缓冲区的双端队列（缓存友好）  [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/CircularDeque.cs)
- `ValueLinkedList` —— 零 GC 链表  [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/ValueLinkedList.cs)

---

### 🌐 网络

- `ValueHttpServer`  
  - Windows: IOCP  
  - macOS: kqueue  
  - Linux: io_uring / epoll  
  - 零 GC HTTP/1.1 服务器  
  - [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/HttpServer.cs)

- `ValueHttpClient`  
  - 零 GC HTTP 客户端  
  - 支持进程间通信（IPC）  
  - [异步范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/HttpClientAsync.cs)
  - [同步范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/HttpClientSync.cs)

> ⚠️ 当前不支持 TLS / HTTP2 / HTTP3，建议使用 Nginx / Caddy 作为反向代理。

---

### ⚙️ 系统与运行时

- `NativeThread` —— 原生线程封装  [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/Threads.cs)
- `NativeThreadPool` —— 线程池  [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/ThreadPool.cs)
- `Coroutine` —— 协程机制  [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/Coroutine.cs)
- `ValueTask` —— 零 GC 异步封装（实验性，最多支持 64 个任务）[范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/AsyncWithThreads.cs)
- `BaseMemoryPool` —— 通用内存池 [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/BaseMemoryPool.cs)
- `MemoryPoolCluster` —— 静态内存池集群 [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/MemoryPoolCluster.cs)

---

### 💾 文件 IO

- `FilesIO` —— 同步文件读写  [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/FilesIO_SYNC.cs)
- `AsyncFilesIO` —— 异步（阻塞模型）  [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/FilesIO.cs)
- `AsyncFilesIOWithCallBack` —— 异步（回调模型） [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/FilesIOWithCallBack.cs)

---

### 🧾 序列化

- `JsonDocument` —— JSON 解析器  
- `SolamirareJsonGenerator` —— 高性能 JSON 序列化工具  
- [范例](https://github.com/liangyichen/Solamirare/blob/main/Samples/Sample_Json.cs)
---

### 🧰 工具函数

- `RandomStringGenerator`  
- `RandomNumberGenerator`  
- `ValueTypeHelper`  
  - StartsWith / EndsWith  
  - IndexOf / LastIndexOf  
  - 忽略大小写版本  
  - HashCode / Equals  

---

## ⚠️ 重要说明

本库基于 **unsafe 代码与手动内存管理**。

- 必须手动调用 `Dispose()` 释放内存  
- 指针使用不当可能导致进程崩溃  
- 不适合初学者使用  

如果你的项目不允许 unsafe，则无法使用本库。

---

## 🚫 使用限制

- 禁止通过 `UnManagedMemory<char>` 修改字符串常量（字符串驻留池问题）  
- 禁止在集合扩容后继续使用旧指针  
- 不支持 32 位系统  
- 未测试 Intel macOS  
- 1.x 版本不支持 FreeBSD  

---

## 🧪 平台支持

- Windows 10+（x64 / ARM64）  
- macOS（Apple Silicon 15.6+）  
- Linux（Fedora 43+，Kernel 6.15+）  

> 在较低版本 Linux 内核中，io_uring 会自动降级为 epoll。

---

## 🔢 版本说明

- `1.0.0-alpha` —— 开发阶段，API 不稳定  
- `1.0.0-beta` —— 功能冻结，可能存在问题  
- `1.0.0-rc` —— 发布候选版本  
- `1.0.0` —— 正式版  
- `1.x.x` —— 修复版本  

---

## 🔮 未来计划

- [ ] FreeBSD 支持  
- [ ] 基于协程的 HTTP 服务器  
- [ ] TCP / UDP 服务器  
- [ ] WebSocket  
- [ ] Flat Hash Map（Swiss Table）  
- [ ] XML 序列化  
