using System;
using System.Collections.Generic;
using System.Text;

namespace Solamirare;


public static unsafe partial class UnManagedStringHelper
{




    /// <summary>
    /// 将 JSON 数组字符串反序列化为非托管字符串集合。
    /// </summary>
    /// <param name="jsonSource">JSON 数组字符串。</param>
    /// <returns>解析后的字符串集合。</returns>
    public static UnManagedMemory<UnManagedString> JsonCollectionDecode(this ReadOnlySpan<char> jsonSource)
    {
        if (JsonValidator.IsValidJson(jsonSource) && jsonSource[0] == '[')
        {
            UnManagedString mem = jsonSource.MapToUnManagedMemory();

            UnManagedMemory<UnManagedString> result = new UnManagedMemory<UnManagedString>();

            SolamirareJsonGenerator.DecodeJsonCollection(&mem, &result);

            return result;
        }

        return UnManagedMemory<UnManagedString>.Empty;
    }


}