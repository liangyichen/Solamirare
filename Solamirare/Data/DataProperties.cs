

namespace Solamirare
{ 

    /// <summary>
    /// 数据属性（基于一对多用途，它本身不设关联绑定）
    /// </summary>
   public  record DataProperty
    {
        /// <summary>
        /// 数据属性（基于一对多用途，它本身不设关联绑定）
        /// </summary>
        public DataProperty()
        {
            Name = string.Empty;
            Value = string.Empty;
        }


        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; }



    }
}
