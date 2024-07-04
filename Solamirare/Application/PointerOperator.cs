namespace Solamirare;


    /// <summary>
    /// 指针操作
    /// </summary>
    public static unsafe class PointerOperator
    {
        /// <summary>
        /// 通过指针改变字符串的值，调用者需要自行保证原始字符串长度的安全，value 的数量必须小于等于原始字符串的长度。
        /// 返回值是指针的自增次数，用于给调用者参考（如果这个指针还需要接下去顺序操作的话就要参考这个返回值）。
        /// 函数内部不会改变调用者传递进来的指针之前的指向地址。
        /// </summary>
        public static int SetPointerValue(char* ptr, params char[] value)
        {
            int i = 0;
            for (;i<value.Length;i++)
            {
                *ptr = value[i];
                
                ptr += 1;
            }

            return i;
        }

    }