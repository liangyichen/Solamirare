### [Solamirare](Solamirare.md 'Solamirare').[CircularDeque&lt;T&gt;](Solamirare.CircularDeque_T_.md 'Solamirare\.CircularDeque\<T\>')

## CircularDeque\<T\>\.Index\(int\) Method

核心索引计算：确保索引在 \[0, \_capacity \- 1\] 范围内循环。
<br/>
Core index calculation: ensures the index cycles within the range \[0, \_capacity \- 1\]\.

```csharp
private int Index(int index);
```
#### Parameters

<a name='Solamirare.CircularDeque_T_.Index(int).index'></a>

`index` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

原始索引。
<br/>
Raw index\.

#### Returns
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')  
映射后的缓冲区索引。
<br/>
Mapped buffer index\.