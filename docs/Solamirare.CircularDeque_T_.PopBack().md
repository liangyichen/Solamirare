### [Solamirare](Solamirare.md 'Solamirare').[CircularDeque&lt;T&gt;](Solamirare.CircularDeque_T_.md 'Solamirare\.CircularDeque\<T\>')

## CircularDeque\<T\>\.PopBack\(\) Method

移除并返回位于双端队列末尾的对象。
<br/>
Removes and returns the object at the end of the deque\.

```csharp
public T PopBack();
```

#### Returns
[T](Solamirare.CircularDeque_T_.md#Solamirare.CircularDeque_T_.T 'Solamirare\.CircularDeque\<T\>\.T')  
位于双端队列末尾的对象。
<br/>
The object at the end of the deque\.

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
双端队列为空。
<br/>
The deque is empty\.