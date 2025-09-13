using MediatR;
using qqbot.Abstractions;
using qqbot.Models;
using qqbot.Models.Notifications;
using qqbot.Services;
using System.Text;

namespace qqbot.Helper;

public class HelpHandler :
        ICommandHandler,
        INotificationHandler<GroupMessageReceivedNotification>,
        INotificationHandler<PrivateMessageReceivedNotification>
{
    public CommandDefinition Command => new()
    {
        Name = "/help",
        Description = "显示所有可用的命令列表。",
        Aliases = { "/帮助", "/菜单" }
    };

    private readonly CommandRegistry _commandRegistry;
    private readonly NapCatApiService _napCatApiService;

    public HelpHandler(CommandRegistry commandRegistry, NapCatApiService napCatApiService)
    {
        _commandRegistry = commandRegistry;
        _napCatApiService = napCatApiService;
    }

    public async Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (IsHelpCommand(messageEvent.RawMessage))
        {
            string helpText = BuildHelpMessage();
            var replyMessage = new List<MessageSegment> { MessageSegment.Text(helpText) };
            await _napCatApiService.SendGroupMessageAsync(messageEvent.GroupId, replyMessage);
        }
    }

    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (IsHelpCommand(messageEvent.RawMessage))
        {
            string helpText = BuildHelpMessage();
            var replyMessage = new List<MessageSegment> { MessageSegment.Text(helpText) };
            await _napCatApiService.SendPrivateMessageAsync(messageEvent.UserId, replyMessage);
        }
    }

    private bool IsHelpCommand(string message)
    {
        var commandText = message.Trim();
        return commandText.Equals(Command.Name, System.StringComparison.OrdinalIgnoreCase) ||
               Command.Aliases.Contains(commandText, System.StringComparer.OrdinalIgnoreCase);
    }

    private string BuildHelpMessage()
    {
        var builder = new StringBuilder();
        builder.AppendLine("--- 机器人可用命令 ---");

        // 从 Commands 字典中获取命令定义
        var sortedCommands = _commandRegistry.Commands.Values.OrderBy(c => c.Name);

        foreach (var commandDef in sortedCommands)
        {
            // 调用递归辅助方法来格式化每个命令及其子命令
            FormatCommand(builder, commandDef, 0);
        }
        return builder.ToString();
    }


    /// <summary>
    /// 递归地格式化一个命令及其所有子命令
    /// </summary>
    private void FormatCommand(StringBuilder builder, CommandDefinition commandDef, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2); // 缩进

        // 拼接主命令和所有别名
        var allNames = new List<string> { commandDef.Name };
        allNames.AddRange(commandDef.Aliases);
        builder.Append(indent).Append(string.Join(" ", allNames));

        // 拼接描述
        builder.Append(" : ").AppendLine(commandDef.Description);

        // 拼接参数
        if (commandDef.Arguments.Any())
        {
            builder.Append(indent).AppendLine("  参数 (args):");
            foreach (var arg in commandDef.Arguments)
            {
                string requiredText = arg.IsRequired ? "" : " (可选)";
                builder.Append(indent).AppendLine($"    {arg.Name}{requiredText} : {arg.Description}");
            }
        }

        // 递归处理子命令
        if (commandDef.SubCommands.Any())
        {
            builder.Append(indent).AppendLine(" (sub):");
            foreach (var subCommand in commandDef.SubCommands.OrderBy(sc => sc.Name))
            {
                // 递归调用，缩进+1
                FormatCommand(builder, subCommand, indentLevel + 1);
            }
        }
    }


}