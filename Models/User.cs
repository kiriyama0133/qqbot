using System.Text.Json.Serialization;
namespace qqbot.Models;

public class User
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
    /// 包含具体账号信息的数据对象
    /// </summary>
    [JsonPropertyName("data")]
    public AccountInfoData? Data { get; set; } // 使用可空类型，以防 data 字段不存在

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
    /// 回显字段，通常与请求中的 echo 字段相同
    /// </summary>
    [JsonPropertyName("echo")]
    public string Echo { get; set; } = string.Empty;
}

/// <summary>
/// 具体的账号信息
/// </summary>
public class AccountInfoData
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    /// <summary>
    /// QQ号 (字符串形式)
    /// </summary>
    [JsonPropertyName("uin")]
    public string Uin { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 年龄
    /// </summary>
    [JsonPropertyName("age")]
    public int Age { get; set; }

    /// <summary>
    /// QID
    /// </summary>
    [JsonPropertyName("qid")]
    public string Qid { get; set; } = string.Empty;

    /// <summary>
    /// QQ等级
    /// </summary>
    [JsonPropertyName("qqLevel")]
    public int QqLevel { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [JsonPropertyName("sex")]
    public string Sex { get; set; } = string.Empty;

    /// <summary>
    /// 长昵称
    /// </summary>
    [JsonPropertyName("long_nick")]
    public string LongNick { get; set; } = string.Empty;

    /// <summary>
    /// 注册时间 (可能是Unix时间戳)
    /// </summary>
    [JsonPropertyName("reg_time")]
    public long RegistrationTime { get; set; }

    /// <summary>
    /// 是否是VIP
    /// </summary>
    [JsonPropertyName("is_vip")]
    public bool IsVip { get; set; }

    /// <summary>
    /// 是否是年费VIP
    /// </summary>
    [JsonPropertyName("is_years_vip")]
    public bool IsYearsVip { get; set; }

    /// <summary>
    /// VIP 等级
    /// </summary>
    [JsonPropertyName("vip_level")]
    public int VipLevel { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [JsonPropertyName("remark")]
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// 登录天数
    /// </summary>
    [JsonPropertyName("login_days")]
    public int LoginDays { get; set; }
}