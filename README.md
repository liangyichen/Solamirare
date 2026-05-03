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

---

## 📚 Documentation

https://github.com/liangyichen/Solamirare/blob/main/docs/Solamirare.md