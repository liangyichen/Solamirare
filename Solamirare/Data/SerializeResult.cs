using System.Runtime.CompilerServices;


namespace Solamirare
{



    /// <summary>
    /// 序列化执行结果
    /// </summary>
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi, Pack = 4)]
    public ref struct SerializeResult
    {
        /// <summary>
        /// 执行结果状态码
        /// </summary>
        [FieldOffset(0)]
        public SerializeResultEnum Status;

        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool Success
        {
            get
            {
                return Status == SerializeResultEnum.OK;
            }
        }

    }

}
