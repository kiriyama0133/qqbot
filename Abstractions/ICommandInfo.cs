using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qqbot.Abstractions
{
    /// <summary>
    /// 一个接口，所有可被帮助命令发现的处理器都应实现它。
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// 定义此 Handler 能够处理的命令结构
        /// </summary>
        CommandDefinition Command { get; }
    }
}
