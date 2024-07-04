
namespace Solamirare
{
    /// <summary>
    /// 动态数据节点 (没有id,需要包裹在字典里)
    /// </summary>
    public record DynamicData:Photon
    {

        /// <summary>
        /// 动态数据节点
        /// </summary>
        public DynamicData()
        {
            T = 0;
            S = 0;
        }

        /// <summary>
        /// 所属域
        /// </summary>
        public int DomainId { get; set; }

        /// <summary>
        /// "Type" 的简称，表示业务【 类别 】。 0 表示最普通的动态数据，负数表示系统使用的部分，不允许删除.
        /// 对应 DynamicApplicationType 的静态属性值
        /// </summary>
        public int T { get; set; }

        /// <summary>
        /// "Status" 的简称，表示对象【 状态 】，一般情况下不会用到，默认值是 0。 用于给 T 作附加标注。
        /// 例如相同 T 值的两个对象， 其中一个 S 为 0, 另一个为 1， 可以认为标注为 S=0 的对象是关闭， S=1 的对象是开启。
        /// 具体用法由业务自行决定。
        /// </summary>
        public int S {get;set;}

        /// <summary>
        /// 该条数据是否标记为空值（ T 对应 DynamicApplicationType 的静态属性值 ）
        /// </summary>
        public bool IsEmptyValue {

            get
            {
                return T == DynamicApplicationType.IsEmpty;
            }
        }
    }
}