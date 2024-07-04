

namespace Solamirare
{



    /// <summary>
    /// 带有Id的数据核心
    /// </summary>
    public record DataCore:Photon
    {


        /// <summary>
        /// 唯一标识，默认情况会自动生成
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 带有Id的数据核心
        /// </summary>
        public DataCore() : base()
        {
            Id = Guid.NewGuid();
        }
    }
}
