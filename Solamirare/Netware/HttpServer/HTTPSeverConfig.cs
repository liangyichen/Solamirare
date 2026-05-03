using System;
using System.Collections.Generic;
using System.Text;

namespace Solamirare;




/// <summary>
/// HTTP 服务器设置
/// </summary>
public unsafe ref struct HTTPSeverConfig
{




    /// <summary>
    /// 服务器对象实例指针，该字段只有在服务器对象创建后才会被赋值。
    /// </summary>
    internal UnManagedMemory<nint> Instances;

    internal MemoryPoolCluster MemoryPool;

    /// <summary>
    /// 启动器
    /// </summary>
    internal ValueHttpServer* Starter;

    /// <summary>
    /// Per-connection recv buffer bytes / 单连接接收缓冲区字节数。
    /// <para>Nginx、Apache、Tomcat 是 8KB, IIS、NodeJs 是 16KB，Kestrel 是 32KB</para>
    /// </summary>
    internal uint READ_BUFFER_CAPACITY;


    /// <summary>
    /// Per-connection response buffer bytes / 单连接响应缓冲区字节数。
    /// </summary>
    internal int RESPONSE_BUFFER_CAPACITY;

    /// <summary>
    /// 最大连接数
    /// </summary>
    internal int MAX_CONNECTIONS;

    /// <summary>
    /// 绑定端口
    /// </summary>
    internal ushort Port;

    /// <summary>
    /// 客户端输出逻辑回调函数，返回值表示是否继续保持连接
    /// <para>真实形态：delegate*&lt;UHttpContext*, bool&gt;;</para>
    /// </summary>
    internal delegate*<UHttpContext*, bool> ResponseCallback;


}
