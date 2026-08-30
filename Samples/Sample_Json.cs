
/// <summary>
/// Json 的操作范例
/// </summary>
public static unsafe class Sample_Json
{
    /// <summary>
    /// 通过构造节点的方式组合 Json 字符串
    /// </summary>
    public static void CreateFromNode()
    {
        JsonDocument doc = new JsonDocument();

        doc.AddChild("name", "John", JsonSerializeTypes.String);
        doc.AddChild("age", "1", JsonSerializeTypes.Number);
        doc.AddChild("age", "2", JsonSerializeTypes.Number);
        doc.AddChild("age", "3", JsonSerializeTypes.Number);
        doc.AddChild("age", "4", JsonSerializeTypes.Number);
        doc.AddChild("age", "5", JsonSerializeTypes.String);
        doc.AddChild("age", "6", JsonSerializeTypes.Number);


        UnManagedMemory<char> exportedJson = doc.Serialize();

        Console.WriteLine(exportedJson.AsSpan());

        bool validate = exportedJson.Equals("""{"name":"John","age":1,"age":2,"age":3,"age":4,"age":"5","age":6}""");

        exportedJson.Dispose();

        doc.Dispose();
    }


    /// <summary>
    /// JsonDocument 与 Json 字符串互相转换
    /// </summary>
    public static void CreateFromString()
    {
        ReadOnlySpan<char> rawJson = jsonSource;


        JsonDocument doc = new JsonDocument(rawJson, 16);

        if (doc.Root == null || doc.Root->Type == JsonSerializeTypes.Undefined)
        {
            return;
        }

        UnManagedCollection<char> actualSerializedJson = doc.Serialize();

        // 为原始 JSON 的压缩分配临时缓冲区
        char* tempBuffer1 = (char*)NativeMemory.AllocZeroed((nuint)rawJson.Length * sizeof(char));

        UnManagedCollection<char> originalCompact = JsonDocument.CompactJson(rawJson, tempBuffer1).CompactedData;

        bool validate = originalCompact.Equals(actualSerializedJson);

        NativeMemory.Free(tempBuffer1);

        bool success = doc.ParseSuccess;

        doc.Dispose();

    }


    static string jsonSource = """{"age":100,"str":"str-value","id":999,"number":999.777,"enable":true,"disabled":false}""";

    /// <summary>
    /// Json 字符串与字典的互相转换
    /// </summary>
    public static void StringToDictionary()
    {

        UnManagedMemory<char> mem = jsonSource.CopyToChars();

        ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>> dic = mem.JsonObjectStringToDictionary();

        foreach (DictionarySlot<UnManagedString, UnManagedString>* i in dic)
        {
            Console.WriteLine($"key:{i->Key.AsSpan()}, value:{i->Value.AsSpan()}");
        }

        //字典序列化到 json 字符串
        UnManagedMemory<char> jsonString = dic.SerializeToJson();

        Console.WriteLine(jsonString.AsSpan());

        // 以下的 validate 值会是 false，因为字典保存各个节点是乱序的，与原始的 json 字符串排列顺序不一致，会输出类似以下的字符串：
        // {"disabled":false,"number":999.777,"age":100,"str":"str-value","enable":true,"id":999}
        // 虽然如此，各个 node 节点键值对肯定会与原始 json 中的键值对正确对应

        bool validate = jsonString.Equals(jsonSource);

        jsonString.Dispose();

        foreach (DictionarySlot<UnManagedString, UnManagedString>* i in dic)
        {
            i->Key.Dispose();
            i->Value.Dispose();
        }

        mem.Dispose();
    }

}

