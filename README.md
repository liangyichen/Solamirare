# Solamirare

Zero-GC infrastructure for .NET — unsafe memory, lock-free data structures, and ultra-low-latency networking for performance-critical systems.

面向 .NET 的零 GC 基础设施 —— 提供 unsafe 内存管理、无锁数据结构与超低延迟网络能力，专为性能敏感系统打造。

[English](README.md) | [中文](README.zh-CN.md)
---

## 📦 Installation

```bash
dotnet add package Solamirare
```

NuGet: https://www.nuget.org/packages/Solamirare/

---

## ⚡ Why Solamirare

- 🚫 **Zero GC** — no allocations, no GC pauses  
- ⚙️ **Unsafe memory control** — full control over memory layout and lifetime  
- 🔒 **Lock-free primitives** — designed for high concurrency  
- 🚀 **Ultra-low latency networking** — IOCP / kqueue / io_uring  
- 🧠 **Cache-friendly design** — optimized for modern CPUs  


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

## 🚀 Quick Start

```csharp
unsafe
{
    var memory = new UnManagedMemory<int>(100);

    memory[0] = 42;

    memory.Dispose();
}
```

> ⚠️ You must enable unsafe code in your project:

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```


---

## 🧩 Core Features

### 🧠 Memory & Collections

- `UnManagedCollection<T>` — memory-mapped structures with copy/clone support  
- `UnManagedMemory<T>` — dynamic zero-GC memory with resizing and mutation  
- `UnManagedString` — zero-GC string abstraction  
- [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/UnManagedMemory.cs)
- [Advance Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/AdvanceSample_UnManagedMemory.cs)

---

### 📦 Data Structures

- `ValueDictionary` — zero-GC dictionary (supports heap & mapped modes)  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/ValueDictionary.cs)
- `ValueStack` / `ValueFrozenStack` — high-performance stack variants  [ValueFrozenStack Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/ValueFrozenStack.cs) | [ValueStack Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/ValueStack.cs)
- `VecDeque` — ring-buffer-based deque (cache-friendly)  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/CircularDeque.cs)
- `ValueLinkedList` — zero-GC linked list  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/ValueLinkedList.cs)

---

### 🌐 Networking

- `ValueHttpServer`  
  - IOCP (Windows)  
  - kqueue (macOS)  
  - io_uring / epoll (Linux)  
  - Zero-GC HTTP/1.1 server
  - [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/HttpServer.cs)

- `ValueHttpClient`  
  - Zero-GC HTTP client  
  - Supports IPC scenarios  
  - [Sample Async](https://github.com/liangyichen/Solamirare/blob/main/Samples/HttpClientAsync.cs)
  - [Sample Sync](https://github.com/liangyichen/Solamirare/blob/main/Samples/HttpClientSync.cs)

> ⚠️ TLS / HTTP2 / HTTP3 are not included. Use Nginx / Caddy as reverse proxy.

---

### ⚙️ System & Runtime

- `NativeThread` — native thread abstraction  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/Threads.cs)
- `NativeThreadPool` — thread pooling  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/ThreadPool.cs)
- `Coroutine` — lightweight coroutine system  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/Coroutine.cs)
- `ValueTask` — zero-GC async abstraction (experimental, max 64 tasks)  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/AsyncWithThreads.cs)
- `BaseMemoryPool` —— Dynamic MemoryPool [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/BaseMemoryPool.cs)
- `MemoryPoolCluster` —— Staic MemoryPool Cluster [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/MemoryPoolCluster.cs)

---

### 💾 File I/O

- `FilesIO SYNC` — synchronous file I/O  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/FilesIO_SYNC.cs)
- `AsyncFilesIO` — async (blocking model)  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/FilesIO.cs)
- `AsyncFilesIOWithCallBack` — async (callback model)  [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/FilesIOWithCallBack.cs)

---

### 🧾 Serialization

- `JsonDocument` — JSON parser  
- `SolamirareJsonGenerator` — fast flat JSON serializer  
- [Sample](https://github.com/liangyichen/Solamirare/blob/main/Samples/Sample_Json.cs)
---

### 🧰 Utilities

- `RandomStringGenerator`  
- `RandomNumberGenerator`  
- `ValueTypeHelper`  
  - StartsWith / EndsWith  
  - IndexOf / LastIndexOf  
  - IgnoreCase variants  
  - HashCode / Equals  

---

## ⚠️ Important

This library uses **unsafe code and manual memory management**.

- You MUST manually call `Dispose()` to release memory  
- Pointer misuse may crash your process  
- Not suitable for beginners  

If your project does not allow unsafe code, this library is not for you.

---

## 🚫 Restrictions

- Do NOT modify interned strings via `UnManagedMemory<char>`  
- Do NOT access pointers after collection resize  
- 32-bit systems are NOT supported  
- Intel macOS is NOT tested  
- FreeBSD is NOT supported in 1.x  

---

## 🧪 Platform Support

- Windows 10+ (x64 / ARM64)  
- macOS (Apple Silicon 15.6+)  
- Linux (Fedora 43+, Kernel 6.15+)  

> On older Linux kernels, io_uring will fall back to epoll.

---

## 🔢 Versioning

- `1.0.0-alpha` — unstable, API may change  
- `1.0.0-beta` — feature complete, may contain bugs  
- `1.0.0-rc` — release candidate  
- `1.0.0` — stable release  
- `1.x.x` — patches  

---

## 🔮 Roadmap

- [ ] FreeBSD support  
- [ ] Coroutine-based HTTP server  
- [ ] TCP / UDP server  
- [ ] WebSocket  
- [ ] Flat Hash Map (Swiss Table)  
- [ ] XML serialization  
