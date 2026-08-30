namespace Solamirare;

public unsafe static partial class NativeConsole
{
    /// <summary>
    /// 获取指定索引处的命令行参数。
    /// </summary>
    /// <param name="index">参数索引，从 0 开始，不包含程序路径。</param>
    /// <returns>指定参数的只读字符视图；索引无效时返回空视图。</returns>
    public static ReadOnlySpan<char> GetArguments(int index)
    {
        if (index < 0)
            return default;

        ReadOnlySpan<char> commandLine = Environment.CommandLine.AsSpan();

        int position = SkipProgramName(commandLine);

        for (int i = 0; i <= index; i++)
        {
            if (!TryGetNextArgument(
                commandLine,
                ref position,
                out int start,
                out int length))
            {
                return default;
            }

            if (i == index)
                return commandLine.Slice(start, length);
        }

        return default;
    }


    /// <summary>
    /// 获取当前进程的命令行参数数量。
    /// </summary>
    /// <returns>命令行参数数量，不包含程序路径。</returns>
    public static int ArgumentsCount()
    {
        ReadOnlySpan<char> commandLine = Environment.CommandLine.AsSpan();

        int position = SkipProgramName(commandLine);
        int count = 0;

        while (TryGetNextArgument(
            commandLine,
            ref position,
            out _,
            out _))
        {
            count++;
        }

        return count;
    }


    /// <summary>
    /// 跳过命令行中的程序路径并返回下一个参数的起始位置。
    /// </summary>
    /// <param name="commandLine">完整命令行。</param>
    /// <returns>第一个参数的起始位置。</returns>
    private static int SkipProgramName(ReadOnlySpan<char> commandLine)
    {
        int position = 0;

        while (position < commandLine.Length &&
               !IsWhitespace(commandLine[position]))
        {
            position++;
        }

        return position;
    }

    /// <summary>
    /// 尝试读取命令行中的下一个参数。
    /// </summary>
    /// <param name="commandLine">完整命令行。</param>
    /// <param name="position">当前解析位置，成功后更新为下一个位置。</param>
    /// <param name="start">输出参数在命令行中的起始位置。</param>
    /// <param name="length">输出参数长度。</param>
    /// <returns>成功读取参数返回 true，否则返回 false。</returns>
    private static bool TryGetNextArgument(ReadOnlySpan<char> commandLine,ref int position,out int start,out int length)
    {
        // 跳过空白
        while (position < commandLine.Length &&
               IsWhitespace(commandLine[position]))
        {
            position++;
        }

        if (position >= commandLine.Length)
        {
            start = 0;
            length = 0;
            return false;
        }

        // 引号参数
        if (commandLine[position] == '"')
        {
            position++;

            start = position;

            while (position < commandLine.Length)
            {
                // \"
                if (commandLine[position] == '\\' &&
                    position + 1 < commandLine.Length &&
                    commandLine[position + 1] == '"')
                {
                    position += 2;
                    continue;
                }

                // 未转义的 "
                if (commandLine[position] == '"')
                {
                    length = position - start;
                    position++;
                    return true;
                }

                position++;
            }

            // 没有找到结束引号：
            // 将剩余内容视为参数
            length = position - start;
            return true;
        }

        // 普通参数
        start = position;

        while (position < commandLine.Length &&
               !IsWhitespace(commandLine[position]))
        {
            position++;
        }

        length = position - start;
        return true;
    }

    /// <summary>
    /// 判断字符是否为空白字符。
    /// </summary>
    /// <param name="c">待判断的字符。</param>
    /// <returns>为空白字符返回 true，否则返回 false。</returns>
    private static bool IsWhitespace(char c)
    {
        return char.IsWhiteSpace(c);
    }

}