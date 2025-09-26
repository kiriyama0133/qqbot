using System.Text.Json.Serialization;

namespace qqbot.Models.Entities;

/// <summary>
/// 具体的账号信息
/// </summary>
public class AccountInfoData
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("uin")]
    public string Uin { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("qid")]
    public string Qid { get; set; } = string.Empty;

    [JsonPropertyName("qqLevel")]
    public int QqLevel { get; set; }

    [JsonPropertyName("sex")]
    public string Sex { get; set; } = string.Empty;

    [JsonPropertyName("long_nick")]
    public string LongNick { get; set; } = string.Empty;

    [JsonPropertyName("reg_time")]
    public long RegistrationTime { get; set; }

    [JsonPropertyName("is_vip")]
    public bool IsVip { get; set; }

    [JsonPropertyName("is_years_vip")]
    public bool IsYearsVip { get; set; }

    [JsonPropertyName("vip_level")]
    public int VipLevel { get; set; }

    [JsonPropertyName("remark")]
    public string Remark { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("login_days")]
    public int LoginDays { get; set; }
}
