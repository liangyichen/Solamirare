
namespace Solamirare
{

    /// <summary>
    /// 数据核心
    /// </summary>
    public record Photon
    {
        int length;


        /// <summary>
        /// 数据核心
        /// </summary>
        public Photon()
        {
            Data = new();
            length = 0;
        }


        /// <summary>
        /// 保存的数据
        /// </summary>
        public ConcurrentDictionary<string, string> Data { get; set; }


        /// <summary>
        /// 导出核心数据 (延迟求值方式)(注意反噬)
        /// </summary>
        /// <param name="keys">可选节点名称</param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, string>> Export(params string[] keys)
        {

            //HashSet<string> set = new HashSet<string>(keys);

            //Data的节点全是值类型，（在这里string也可以理解为值类型）， 所以在此不能作立即求值， 否则会创建大量临时内存

            if (keys is not null && keys.Any())
            {
                return Data.Where(i => keys.Contains(i.Key));
            }
            else
            {
                return Data.Select(i => i);
            }

        }




        /// <summary>
        /// 导出 json 对象
        /// </summary>
        /// <param name="iText"></param>
        /// <param name="searchSymbols">是否查找 json 中的特殊符号，如果外部确定不存在违反 json 协议的特殊符号，使用 false 可以获得最高性能</param>
        /// <param name="keys">可选节点名称</param>
        /// <returns></returns>
        public string ExportToJson(ITextSerializer iText, bool searchSymbols = true, params string[] keys)
        {
            IEnumerable<KeyValuePair<string, string>> selected = Export(keys);



            return iText.SerializeObject(selected,keys.Length,searchSymbols);
        }




        /// <summary>
        /// 导出 json 对象。
        /// </summary>
        /// <param name="iText"></param>
        /// <param name="searchSymbols">是否查找 json 中的特殊符号，如果外部确定不存在违反 json 协议的特殊符号，使用 false 可以获得最高性能</param>
        /// <returns></returns>
        public string ExportToJson(ITextSerializer iText, bool searchSymbols = true)
        {
            return iText.SerializeObject(Data,length,searchSymbols);
        }



        /// <summary>
        /// 从键值对集合导入数据，差集部分会添加，交集部分会被忽略（保持旧值）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Photon Import(IEnumerable<KeyValuePair<string, string>> data)
        {
            if(data is not null)
            foreach(var i in data)
            {
                AppendIfNotExist(i.Key, i.Value);
            }

            return this;
        }

        /// <summary>
        /// 与另一个动态数据融合, 交集部分会被更新，差集部分会添加，可选参数
        /// </summary>
        /// <param name="append"></param>
        /// <param name="keys">融合选择的项</param>
        /// <returns></returns>
        public Photon Fusion(Photon append, params string[] keys)
        {
            if (append is not null && append.Data is not null)
            {
                var innerDatas = append.Export(keys);
                foreach(var i in innerDatas)
                    Set(i.Key, i.Value);
            }
            

            return this;
        }
        

        

        // /// <summary>
        // /// 更新一个或多个属性
        // /// </summary>
        // /// <param name="value"></param>
        // public void Update(IEnumerable<DataProperty> value)
        // {

        //     if(value is not null)
        //     {
        //         foreach (var i in value)
        //         {
        //             //为保证 null 判断而引用 Set, 不直接使用  Data[i.Name] = i.Failure_Format_Of_Source_Value;
        //             Set(i.Name, i.Value);

        //         }
        //     }
        // }


        // /// <summary>
        // /// 获取所有属性
        // /// </summary>
        // public IEnumerable<DataProperty> Values()
        // {
        //     return Data.Select(i => new DataProperty { Name = i.Key, Value = i.Value as string });
        // }


        // /// <summary>
        // /// 获取所有属性
        // /// </summary>
        // public IEnumerable<KeyValuePair<string,string>> Values2()
        // {
        //     return Data.Select(i => i);
        // }



        /// <summary>
        /// 获取属性, 不会有 null，如果逻辑为空，则得到 string.Empty
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string Get(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Data.TryGetValue(name, out var value);
                if (value is not null)
                {
                    var ret = value.ToString();
                     
                    return  ret;
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }


        /// <summary>
        /// 如果不存在该属性，则添加， 如果已经存在，则什么都不会执行. （如果value为null，则写入string.Empty）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Photon AppendIfNotExist(string name, string value)
        {
            //空字符和空白符都不允许作为key
            if (!string.IsNullOrWhiteSpace(name) && !Data.ContainsKey(name))
            {
                
                if (Data.TryAdd(name, value is null ? string.Empty : value)) length += 1;
            }

            return this;
        }


        /// <summary>
        /// 设置属性，存在则更新，如果不存在则添加（如果 value 为 null，则写入 string.Empty）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Photon Set(string name, string value)
        {
            //空字符和空白符都不允许作为 key
            if (!string.IsNullOrWhiteSpace(name))
            {
                //业务有可能需要写入空字符，所以这里只做 null 判断
                string append = value is null ? string.Empty : value;


                if (Data.ContainsKey(name))
                {
                    var existing = Data[name];

                    // TryUpdate 才能进行线程安全的更新， Data[name] = xxx 并非线程安全

                    bool success = false;
                    int retryCount = 0;

                    tryUpdate:

                    success = Data.TryUpdate(name, append, existing);
                    retryCount += 1;

                    if (!success && retryCount == 1)  //遇到多线程同时写问题，重试 1 次， 不可能有尝试2次的机率
                        goto tryUpdate;

                }
                else
                { 
                    if(Data.TryAdd(name, append)) length += 1;
                }
            }

            return this;
        }


        /// <summary>
        /// 移除属性
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Photon Remove(string name)
        {

            if(!string.IsNullOrWhiteSpace(name))
            {
                if (Data.ContainsKey(name)) 
                {
                    if (Data.TryRemove(name, out _)) length -= 1;
                }
            }

            return this;
        }


        /// <summary>
        /// 获取 Data 的数据条目（ 绝大部分场合用于代替 Data.Count() ）
        /// (根据.net源码显示，如果获取 ConcurrentDictionary 的 Count属性 会进行两次锁操作、以及一次循环计算，成本太大，所以在这里自己做统计实现0查找)
        /// https://source.dot.net/#System.Collections.Concurrent/System/Collections/Concurrent/ConcurrentDictionary.cs,158
        /// </summary>
        public int Length
        {
            get
            {
                return length;
            }
        }


    }



}
