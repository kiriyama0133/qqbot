using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qqbot.Abstractions;

/// <summary>
/// 定义一个命令的参数
/// </summary>
public class CommandArgument
{
    public string Name { get; set; } = string.Empty; // 参数名, e.g., "user"
    public string Description { get; set; } = string.Empty; // 参数描述, e.g., "要@的用户"
    public bool IsRequired { get; set; } = true; // 是否为必需参数
}

/// <summary>
/// 完整地定义一个命令，支持子命令
/// </summary>
public class CommandDefinition
{
    public string Name { get; set; } = string.Empty; // 主命令名, e.g., "/admin"
    public string Description { get; set; } = string.Empty; // 功能描述
    public List<string> Aliases { get; set; } = new(); // 别名, e.g., ["/管理员"]
    public List<CommandArgument> Arguments { get; set; } = new(); // 参数列表
    public List<CommandDefinition> SubCommands { get; set; } = new(); // 子命令列表
}
