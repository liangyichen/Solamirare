namespace Solamirare
{
    /// <summary>
    /// 字符串操作接口
    /// </summary>
    public interface IIntellStringReplace
    {
        /// <summary>
        /// 高效字符串替换，在查询值与新值等长的情况下可以实现 0 分配内存， 如果两个输入值不等长，在做连续大量替换的时候也能发挥高性能。
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="selectString"></param>
        /// <param name="newString"></param>
        void Replace(ref string sourceString, string selectString, string newString);



        /// <summary>
        /// 高效字符串替换，已经假定查询值与新值等长，并且不做 null 判断。
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="selectString"></param>
        /// <param name="newString"></param>
        /// <param name="lengthOfSelectValue"></param>
        void UnsafeReplace(ref string sourceString, ref string selectString, ref string newString, int lengthOfSelectValue);
    }
}