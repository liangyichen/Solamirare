namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_this"></param>
    public static implicit operator UnManagedCollection<T>(in UnManagedMemory<T> _this)
    {
        if (!_this.activated) return UnManagedCollection<T>.Empty;

        return _this.Prototype;
    }





    /// <summary>
    /// 
    /// </summary>
    /// <param name="managedString"></param>
    public static implicit operator UnManagedMemory<T>(ReadOnlySpan<T> managedString)
    {
        UnManagedMemory<T> mem = managedString.MapToUnManagedMemory();

        return mem;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="_this"></param>
    public static implicit operator Span<T>(in UnManagedMemory<T> _this)
    {
        if (!_this.activated) return Span<T>.Empty;

        Span<T> span = _this.AsSpan();

        return span;
    }


}