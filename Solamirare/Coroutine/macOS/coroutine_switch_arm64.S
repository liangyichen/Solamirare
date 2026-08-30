.text
.align 4

// ── coroutine_switch ──────────────────────────────────────────────────────────
// void coroutine_switch(CoroutineContext* from, CoroutineContext* to)
// x0 = from, x1 = to
.global _coroutine_switch
_coroutine_switch:

    // 保存当前寄存器到 from（x0）
    stp  x19, x20, [x0, #0]
    stp  x21, x22, [x0, #16]
    stp  x23, x24, [x0, #32]
    stp  x25, x26, [x0, #48]
    stp  x27, x28, [x0, #64]
    stp  x29, x30, [x0, #80]
    mov  x9,  sp
    str  x9,  [x0, #96]

    // 恢复 to（x1）的寄存器
    ldp  x19, x20, [x1, #0]
    ldp  x21, x22, [x1, #16]
    ldp  x23, x24, [x1, #32]
    ldp  x25, x26, [x1, #48]
    ldp  x27, x28, [x1, #64]
    ldp  x29, x30, [x1, #80]
    ldr  x9,       [x1, #96]
    mov  sp,  x9

    ret

// ── coroutine_entry_trampoline ────────────────────────────────────────────────
// 协程首次启动的跳板函数
// 通过 callee-saved 寄存器接收参数（由 InitContext 写入）：
//   x19 = 入口函数指针 (entry)
//   x20 = 入口函数参数 (param)
//   x21 = 完成回调函数指针 (finishCallback，[UnmanagedCallersOnly] C# 函数)
//   x22 = 调度器指针 (scheduler)
.global _coroutine_entry_trampoline
_coroutine_entry_trampoline:

    // 调用真正的入口函数：entry(param)
    // x0 = param（第一个参数）
    mov  x0, x20
    blr  x19

    // 入口函数返回，调用完成回调：finishCallback(scheduler)
    // x0 = scheduler
    mov  x0, x22
    blr  x21

    // finishCallback 不会返回（它会切换上下文到调用方）
    // 以防万一，触发断点中止
    brk  #0