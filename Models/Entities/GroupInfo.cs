using System.Text.Json.Serialization;

namespace qqbot.Models.Entities;

/// <summary>
/// 代表单个群聊的信息
/// </summary>
public class GroupInfo
{
    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("group_name")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }

    [JsonPropertyName("max_member_count")]
    public int MaxMemberCount { get; set; }

    [JsonPropertyName("group_all_shut")]
    public int GroupAllShut { get; set; }

    [JsonPropertyName("group_remark")]
    public string GroupRemark { get; set; } = string.Empty;
}
