namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 定义从 UnManagedCollection&lt;T&gt; 到 Span&lt;T&gt; 的隐式转换。
    /// <para>Defines an implicit conversion from UnManagedCollection&lt;T&gt; to Span&lt;T&gt;.</para>
    /// </summary>
    /// <param name="_this">要转换的 UnManagedCollection&lt;T&gt; 实例。<para>The UnManagedCollection&lt;T&gt; instance to convert.</para></param>
    public static implicit operator Span<T>(UnManagedCollection<T> _this)
    {
        Span<T> span = _this.AsSpan();

        return span;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="_this"></param>
    public static implicit operator UnManagedCollection<T>(ReadOnlySpan<T> _this)
    {
        return _this.MapToUnManagedCollection();
    }

     


}