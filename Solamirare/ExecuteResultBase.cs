
namespace Solamirare
{

    // struct 不能继承，所以泛型部分只能写重复代码


    /// <summary>
    /// 更轻量级的操作结果，用于逐渐淘汰 ExecuteResultBase
    /// </summary>
    public ref struct ProcessResult
    {

        /// <summary>
        /// 包含的数据
        /// </summary>
        public int Data;

        /// <summary>
        /// 操作结果
        /// </summary>
        public ProcessResult()
        {
            _message = -1;
            Data = 0;
            Success = true;
        }
        

        /// <summary>
        /// 执行描述（如果值不是-1，则Success自动为false），
        /// 如果值再次设置为-1时，则Success再次为true
        /// </summary>
        public int MessageCode
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                Success = _message == -1;
            }
        }

        int _message;

        /// <summary>
        /// 是否执行成功(默认值是 true, MessageCode 不为-1时会自动变为false)
        /// </summary>
        public bool Success;

        /// <summary>
        /// 转换为 Json 字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var success = Success?"true":"false"; //必须转换为小写，要不然会成为 True False, json格式错误
            return $"{{\"Success\":{success},\"MessageCode\":{_message}, \"Data\":{Data}}}";
        }
    }

    /// <summary>
    /// 操作结果
    /// </summary>
    public class ExecuteResultBase
    {
        /// <summary>
        /// 操作结果
        /// </summary>
        public ExecuteResultBase()
        {
            _message = string.Empty;
            Success = true;
        }

        /// <summary>
        /// 操作结果
        /// </summary>
        /// <param name="action">Success为true的条件</param>
        /// <param name="message">如果Success为false，那么应该在这里输入原因</param>
        public ExecuteResultBase(bool action, string message)
        {
            Success = action;
            if (!Success) 
                _message = message;
            else
                _message = string.Empty;
        }

        /// <summary>
        /// 执行描述（如过值不是string.Empty，则Success自动为false），
        /// 同时，如果值再次设置为string.Empty，则Success再次为true
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                Success = _message == string.Empty;
            }
        }

        string _message;

        /// <summary>
        /// 是否执行成功(默认值是true,Message非空时会自动变为false)
        /// </summary>
        public bool Success { get; private set; }


    }

    /// <summary>
    /// 操作结果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExecuteResultBase<T> : ExecuteResultBase
    {
        /// <summary>
        /// 包含的数据
        /// </summary>
        public T? Core { get; set; }
    }




}
