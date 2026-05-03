namespace Solamirare
{



    /// <summary>
    /// 字典功能扩展
    /// </summary>
    public static unsafe partial class UDictionaryExtension
    {

        /// <summary>
        /// 字符串形式键值对字典释放所有键值对指向内存，以及键值对本身
        /// </summary>
        /// <param name="target"></param>
        public static void DisposeAll(in this ValueDictionary<UnManagedString, UnManagedString> target)
        {
            target.ForEach(&jsonDictionaryDisposeKV, null);
            target.Dispose();
        }


        static bool jsonDictionaryDisposeKV(int index, UnManagedMemory<char>* key, UnManagedMemory<char>* value, void* arg)
        {
            if(key is not null) key->Dispose();
            if(value is not null) value->Dispose();

            return true;
        }


        /// <summary>
        /// 把字典序列化到json
        /// <para>Serializes the dictionary to JSON.</para>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="onMemoryPool">是否使用内存池 <para>Whether to use memory pool</para></param>
        /// <returns>包含 JSON 字符串的非托管内存 <para>Unmanaged memory containing the JSON string</para></returns>
        public static UnManagedString SerializeToJson(in this ValueDictionary<UnManagedString, UnManagedString> target, bool onMemoryPool = false)
        {
            if (target._disposed || target._ctrl == null) return UnManagedString.Empty;


            UnManagedString result = new UnManagedString();

            fixed (ValueDictionary<UnManagedString, UnManagedString>* p = &target)
            {
                SolamirareJsonGenerator.SerializeObject(p, &result);
            }

            return result;


        }


        /// <summary>
        /// 把对象形式的 Json 反序列化到字典
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ValueDictionary<UnManagedString, UnManagedString> JsonObjectStringToDictionary(this ReadOnlySpan<char> source)
        {
            if (JsonValidator.IsValidJson(source) && source[0] == '{')
            {
                ValueDictionary<UnManagedString, UnManagedString> result
                 = new ValueDictionary<UnManagedString, UnManagedString>();

                SolamirareJsonGenerator.DecodeObjectString_AppendToDictionary(source, &result);

                return result;
            }

            return ValueDictionary<UnManagedString, UnManagedString>.Empty;
        }

        /// <summary>
        /// 把对象形式的 Json 反序列化到字典
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ValueDictionary<UnManagedString, UnManagedString> JsonObjectStringToDictionary(this in UnManagedString source)
        {
            if (JsonValidator.IsValidJson(source) && *source[0] == '{')
            {
                ValueDictionary<UnManagedString, UnManagedString> result
                = new ValueDictionary<UnManagedString, UnManagedString>();

                SolamirareJsonGenerator.DecodeObjectString_AppendToDictionary(source, &result);

                return result;
            }

            return ValueDictionary<UnManagedString, UnManagedString>.Empty;
        }


        /// <summary>
        /// 把字典序列化到json
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static UnManagedString SerializeToJson(this in ValueDictionary<UnManagedString, UnManagedString> source)
        {
            UnManagedString result = new UnManagedString();

            fixed (ValueDictionary<UnManagedString, UnManagedString>* p_source = &source)
            {
                SolamirareJsonGenerator.SerializeObject(p_source, &result);
            }

            return result;
        }




    }
}
