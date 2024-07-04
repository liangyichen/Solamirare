
namespace Solamirare;

/// <summary>
/// 
/// </summary>
public unsafe ref struct NodeSchema
{ 

    /// <summary>
    /// 当前字符串在主字符串中的起始位置
    /// </summary>
    public int indexEach;

    /// <summary>
    /// 字符串的原始的、未做特殊符号转换之前的长度
    /// </summary>
    public int subSourceLength;

    
    /// <summary>
    /// 字符串地址
    /// </summary>
    public char* SubString;
    
    
    /// <summary>
    /// 这一整段字符串是否存在特殊字符
    /// </summary>
    public bool ExistSpecialSymbols;




    /// <summary>
    /// 特殊字符的数量
    /// </summary>
    public int SpecialSymbolsCount;
}


