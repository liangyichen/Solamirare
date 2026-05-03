### [Solamirare](Solamirare.md 'Solamirare')

## CircularDeque\<T\> Struct

双端队列，支持在两端高效地添加和移除元素。
<br/>
Double\-ended queue, supports efficient addition and removal of elements at both ends\.

```csharp
public struct CircularDeque<T>
    where T : unmanaged
```
#### Type parameters

<a name='Solamirare.CircularDeque_T_.T'></a>

`T`

队列中元素的类型。
<br/>
The type of elements in the deque\.

| Constructors | |
| :--- | :--- |
| [CircularDeque\(int, bool\)](Solamirare.CircularDeque_T_.CircularDeque(int,bool).md 'Solamirare\.CircularDeque\<T\>\.CircularDeque\(int, bool\)') | 初始化 CircularDeque 类的新实例。 <br/> Initializes a new instance of the CircularDeque class\. |

| Properties | |
| :--- | :--- |
| [Capacity](Solamirare.CircularDeque_T_.Capacity.md 'Solamirare\.CircularDeque\<T\>\.Capacity') | 获取双端队列的容量。 <br/> Gets the capacity of the deque\. |
| [Count](Solamirare.CircularDeque_T_.Count.md 'Solamirare\.CircularDeque\<T\>\.Count') | 获取双端队列中包含的元素数。 <br/> Gets the number of elements contained in the deque\. |

| Methods | |
| :--- | :--- |
| [Clear\(\)](Solamirare.CircularDeque_T_.Clear().md 'Solamirare\.CircularDeque\<T\>\.Clear\(\)') | 从双端队列中移除所有对象。 <br/> Removes all objects from the deque\. |
| [Dispose\(\)](Solamirare.CircularDeque_T_.Dispose().md 'Solamirare\.CircularDeque\<T\>\.Dispose\(\)') | 释放由双端队列使用的所有资源。 <br/> Releases all resources used by the deque\. |
| [Index\(int\)](Solamirare.CircularDeque_T_.Index(int).md 'Solamirare\.CircularDeque\<T\>\.Index\(int\)') | 核心索引计算：确保索引在 \[0, \_capacity \- 1\] 范围内循环。 <br/> Core index calculation: ensures the index cycles within the range \[0, \_capacity \- 1\]\. |
| [PopBack\(\)](Solamirare.CircularDeque_T_.PopBack().md 'Solamirare\.CircularDeque\<T\>\.PopBack\(\)') | 移除并返回位于双端队列末尾的对象。 <br/> Removes and returns the object at the end of the deque\. |
| [PopFront\(\)](Solamirare.CircularDeque_T_.PopFront().md 'Solamirare\.CircularDeque\<T\>\.PopFront\(\)') | 移除并返回位于双端队列开头的对象。 <br/> Removes and returns the object at the beginning of the deque\. |
| [PushBack\(T\)](Solamirare.CircularDeque_T_.PushBack(T).md 'Solamirare\.CircularDeque\<T\>\.PushBack\(T\)') | 将对象添加到双端队列的末尾。 <br/> Adds an object to the end of the deque\. |
| [PushFront\(T\)](Solamirare.CircularDeque_T_.PushFront(T).md 'Solamirare\.CircularDeque\<T\>\.PushFront\(T\)') | 将对象添加到双端队列的开头。 <br/> Adds an object to the beginning of the deque\. |
| [Resize\(int\)](Solamirare.CircularDeque_T_.Resize(int).md 'Solamirare\.CircularDeque\<T\>\.Resize\(int\)') | 调整内部缓冲区的大小。 <br/> Resizes the internal buffer\. |
| [TrimExcess\(\)](Solamirare.CircularDeque_T_.TrimExcess().md 'Solamirare\.CircularDeque\<T\>\.TrimExcess\(\)') | 调整容量，清除所有未使用的容量。 <br/> Sets the capacity to the actual number of elements in the deque \(if it is a power of 2\)\. |
