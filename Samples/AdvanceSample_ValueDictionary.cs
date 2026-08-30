


/// <summary>
/// ValueDictionary 高级用法
/// </summary>
public static unsafe class AdvanceSample_ValueDictionary
{

    /// <summary>
    /// 通过状态码判断当前对象的状态
    /// </summary>
    public static void Sample1()
    {
        ValueDictionary<int, int> dic = new ValueDictionary<int, int>(4);

        dic.Add(0, 1);
        dic.Add(1, 2);
        dic.Add(2, 3);
        dic.Add(3, 4);

        MemoryFingerprint128 code = dic.StatusCode;

        dic.Add(4, 5);

        MemoryFingerprint128 code2 = dic.StatusCode;

        bool validate = code != code2;

        dic.Dispose();
    }



    public static void Sample2()
    {
        ValueDictionary<int, int> dic = new ValueDictionary<int, int>(4);

        dic.Add(0, 1);
        dic.Add(1, 2);
        dic.Add(2, 3);
        dic.Add(3, 4);

        int code = dic.GetHashCode();

        dic.Add(4, 5);

        int code2 = dic.GetHashCode();


        dic.Dispose();
    }

}