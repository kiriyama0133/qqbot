using qqbot.Helper.HttpHandlers;
using qqbot.Models;

namespace qqbot.Services;

public partial class NapCatApiService
{
    /// <summary>
    /// 获取群列表
    /// </summary>
    /// <returns>群信息列表，如果失败则返回 null</returns>
    public async Task<List<GroupInfo>?> GetGroupListAsync()
    {
        try
        {
            // 对于 GET 请求，通常使用 GetFromJsonAsync
            var response = await _httpClient.GetFromJsonAsync<GetGroupListResponse>("/get_group_list");

            if (response != null && response.Status == "ok")
            {
                return response.Data;
            }

            _logger.LogWarning("获取群列表 API 响应状态异常: {Status}", response?.Status);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API 请求返回了一个失败的状态码 (获取群列表)。");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取群列表时发生未知异常。");
            return null;
        }
    }


}
