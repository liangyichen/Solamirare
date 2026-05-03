### [Solamirare](Solamirare.md 'Solamirare')

## SolamirareGlobal Class

```csharp
public static class SolamirareGlobal
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') &#129106; SolamirareGlobal

| Fields | |
| :--- | :--- |
| [BasePool](Solamirare.SolamirareGlobal.BasePool.md 'Solamirare\.SolamirareGlobal\.BasePool') | 动态内存池，用于大对象的分配 |
| [Cluster](Solamirare.SolamirareGlobal.Cluster.md 'Solamirare\.SolamirareGlobal\.Cluster') | 固定大小内存池集群管理器，用于小对象的分配 |

| Methods | |
| :--- | :--- |
| [AlignSize\(ulong\)](Solamirare.SolamirareGlobal.AlignSize(ulong).md 'Solamirare\.SolamirareGlobal\.AlignSize\(ulong\)') | 算法：向上取整到 ALIGNMENT 的最接近倍数。 |
