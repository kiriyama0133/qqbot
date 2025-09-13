using System.Text.Json.Serialization;
using static qqbot.Models.SharedMessageModels;
namespace qqbot.Models;

public class Group
{
    /// <summary>
    /// 代表从 NapCat/OneBot 接收到的实时群聊消息事件
    /// </summary>
    public class GroupMessageEvent
    {
        [JsonPropertyName("post_type")]
        public string PostType { get; set; } = "message";

        [JsonPropertyName("message_type")]
        public string MessageType { get; set; } = "group";

        [JsonPropertyName("sub_type")]
        public string SubType { get; set; } = string.Empty;

        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        [JsonPropertyName("group_id")]
        public long GroupId { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("sender")]
        public SenderInfo? Sender { get; set; }

        [JsonPropertyName("message")]
        public List<MessageSegment> Message { get; set; } = new();

        [JsonPropertyName("raw_message")]
        public string RawMessage { get; set; } = string.Empty;

        [JsonPropertyName("font")]
        public int Font { get; set; }

        [JsonPropertyName("self_id")]
        public long SelfId { get; set; }

        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("echo")]
        public string? Echo { get; set; }
    }


    /// <summary>
    /// 代表群消息撤回的通知事件
    /// </summary>
    public class GroupRecallNoticeEvent
    {
        [JsonPropertyName("post_type")]
        public string PostType { get; set; } = "notice";

        [JsonPropertyName("notice_type")]
        public string NoticeType { get; set; } = "group_recall";

        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("self_id")]
        public long SelfId { get; set; }

        [JsonPropertyName("group_id")]
        public long GroupId { get; set; }

        /// <summary>
        /// 被撤回消息的发送者的 QQ 号
        /// </summary>
        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// 操作者的 QQ 号 (谁执行了撤回操作)
        /// </summary>
        [JsonPropertyName("operator_id")]
        public long OperatorId { get; set; }

        /// <summary>
        /// 被撤回的消息 ID
        /// </summary>
        [JsonPropertyName("message_id")]
        public long MessageId { get; set; }
    }



    /// <summary>
    /// 结构化的消息内容数组
    /// </summary>
    [JsonPropertyName("message")]
        public List<MessageSegment> Message { get; set; } = new();
    }

    /// <summary>
    /// 代表“获取群信息”API的完整响应体
    /// </summary>
    public class GetGroupInfoResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("retcode")]
        public int ReturnCode { get; set; }

        /// <summary>
        /// 包含具体群聊信息的数据对象
        /// </summary>
        [JsonPropertyName("data")]
        public GroupInfo? Data { get; set; } // 注意：这里是单个对象，而不是 List

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("wording")]
        public string Wording { get; set; } = string.Empty;

        [JsonPropertyName("echo")]
        public string? Echo { get; set; }
    }


    /// <summary>
    /// 代表“获取群列表”API的完整响应体
    /// </summary>
    public class GetGroupListResponse
    {
        /// <summary>
        /// 请求状态，通常是 "ok" 或 "error"
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 响应代码
        /// </summary>
        [JsonPropertyName("retcode")]
        public int ReturnCode { get; set; }

        /// <summary>
        /// 包含群聊信息对象的数组
        /// </summary>
        [JsonPropertyName("data")]
        public List<GroupInfo>? Data { get; set; } // List<T> 对应 JSON 数组

        /// <summary>
        /// 提示信息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 提示信息（人性化）
        /// </summary>
        [JsonPropertyName("wording")]
        public string Wording { get; set; } = string.Empty;

        /// <summary>
        /// 回显字段
        /// </summary>
        [JsonPropertyName("echo")]
        public string Echo { get; set; } = string.Empty;
    }

    /// <summary>
    /// 代表单个群聊的信息
    /// </summary>
    public class GroupInfo
    {
        /// <summary>
        /// 群号
        /// </summary>
        [JsonPropertyName("group_id")]
        public long GroupId { get; set; }

        /// <summary>
        /// 群名称
        /// </summary>
        [JsonPropertyName("group_name")]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 成员数量
        /// </summary>
        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        /// <summary>
        /// 最大成员数量
        /// </summary>
        [JsonPropertyName("max_member_count")]
        public int MaxMemberCount { get; set; }

        /// <summary>
        /// 全员禁言状态 (例如 0: 未禁言, -1: 已禁言)
        /// </summary>
        [JsonPropertyName("group_all_shut")]
        public int GroupAllShut { get; set; } // <-- 新增字段

        /// <summary>
        /// 群备注
        /// </summary>
        [JsonPropertyName("group_remark")]
        public string GroupRemark { get; set; } = string.Empty; // <-- 新增字段
    }


