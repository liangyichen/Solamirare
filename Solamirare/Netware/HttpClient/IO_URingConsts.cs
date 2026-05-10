// namespace Solamirare;

// /// <summary>
// /// 提供 Linux io_uring 特有的系统调用号、操作码及内存偏移量常量。
// /// </summary>
// internal static class IO_URingConsts
// {
//     // --- Linux x64 系统调用号 ---

//     /// <summary>io_uring_setup 系统调用号。</summary>
//     internal const long SYS_io_uring_setup = 425;

//     /// <summary>io_uring_enter 系统调用号。</summary>
//     internal const long SYS_io_uring_enter = 426;

//     // --- io_uring 内存映射偏移量 ---

//     /// <summary>提交队列 (SQ) Ring 的映射偏移量。</summary>
//     internal const long IORING_OFF_SQ_RING = 0;

//     /// <summary>完成队列 (CQ) Ring 的映射偏移量。</summary>
//     internal const long IORING_OFF_CQ_RING = 0x8000000;

//     /// <summary>提交队列条目 (SQE) 数组的映射偏移量。</summary>
//     internal const long IORING_OFF_SQES = 0x10000000;

//     // --- io_uring 操作码 (Opcode) ---


//     // --- 运行标志 ---

//     /// <summary>等待并获取完成事件标志。</summary>
//     internal const uint IORING_ENTER_GETEVENTS = 1;
// }