using MediatR;
using qqbot.Abstractions;
using qqbot.Core.Services;
using qqbot.Models.Messages;
using qqbot.Models.Notifications;
using qqbot.Services;
using System.Text;

namespace qqbot.Handlers;

/// <summary>
/// 帮助命令处理器 - 显示所有可用命令列表
/// </summary>
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
    private readonly IDynamicStateService _stateService;

    public HelpHandler(IDynamicStateService stateService, CommandRegistry commandRegistry, NapCatApiService napCatApiService)
    {
        _commandRegistry = commandRegistry;
        _napCatApiService = napCatApiService;
        _stateService = stateService;
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
        return commandText.Equals(Command.Name, StringComparison.OrdinalIgnoreCase) ||
               Command.Aliases.Contains(commandText, StringComparer.OrdinalIgnoreCase);
    }

    private string BuildHelpMessage()
    {
        var builder = new StringBuilder();
        builder.AppendLine("--- 机器人可用命令 ---");

        var commandMap = _stateService.GetState<IReadOnlyDictionary<string, CommandDefinition>>(StateKeys.Commands);

        if (commandMap != null)
        {
            var sortedCommands = commandMap.Values.OrderBy(c => c.Name);
            foreach (var commandDef in sortedCommands)
            {
                FormatCommand(builder, commandDef, 0);
            } 
        }
        return builder.ToString();
    }

    private void FormatCommand(StringBuilder builder, CommandDefinition commandDef, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);

        var allNames = new List<string> { commandDef.Name };
        allNames.AddRange(commandDef.Aliases);
        builder.Append(indent).Append(string.Join(" ", allNames));
        builder.Append(" : ").AppendLine(commandDef.Description);

        if (commandDef.Arguments.Any())
        {
            builder.Append(indent).AppendLine("  参数 (args):");
            foreach (var arg in commandDef.Arguments)
            {
                string requiredText = arg.IsRequired ? "" : " (可选)";
                builder.Append(indent).AppendLine($"    {arg.Name}{requiredText} : {arg.Description}");
            }
        }

        if (commandDef.SubCommands.Any())
        {
            builder.Append(indent).AppendLine(" (sub):");
            foreach (var subCommand in commandDef.SubCommands.OrderBy(sc => sc.Name))
            {
                FormatCommand(builder, subCommand, indentLevel + 1);
            }
        }
    }
}
