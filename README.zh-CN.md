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

---

## 📚 文档

https://github.com/liangyichen/Solamirare/blob/main/docs/Solamirare.md