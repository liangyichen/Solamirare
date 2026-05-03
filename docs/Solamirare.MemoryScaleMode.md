### [Solamirare](Solamirare.md 'Solamirare')

## MemoryScaleMode Enum

内存段扩容模式

```csharp
public enum MemoryScaleMode
```
### Fields

<a name='Solamirare.MemoryScaleMode.X2'></a>

`X2` 0

二倍扩容，直到大于指定的容量

<a name='Solamirare.MemoryScaleMode.AppendEquals'></a>

`AppendEquals` 1

指定容量（但是会保证不会小于原始容量）

<a name='Solamirare.MemoryScaleMode.X3'></a>

`X3` 2

\(原始容量 \+ 指定容量\) \+ \(原始容量 \+ 指定容量\) / 2