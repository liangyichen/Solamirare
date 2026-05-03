### [Solamirare](Solamirare.md 'Solamirare')

## SerializeResultEnum Enum

序列化执行结果状态

```csharp
public enum SerializeResultEnum : System.Byte
```
### Fields

<a name='Solamirare.SerializeResultEnum.OK'></a>

`OK` 0

执行成功

<a name='Solamirare.SerializeResultEnum.Failed_StackReSize'></a>

`Failed_StackReSize` 1

因为栈内存不可重设大小

<a name='Solamirare.SerializeResultEnum.Failed_StackLimit'></a>

`Failed_StackLimit` 2

因为超出栈内存限制

<a name='Solamirare.SerializeResultEnum.Null_Or_Empty'></a>

`Null_Or_Empty` 3

数据源是空值

<a name='Solamirare.SerializeResultEnum.FailedTypes'></a>

`FailedTypes` 4

类型错误

<a name='Solamirare.SerializeResultEnum.UnDefined'></a>

`UnDefined` 5

初始化未定义