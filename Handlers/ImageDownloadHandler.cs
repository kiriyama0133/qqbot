using MediatR;
using qqbot.Models.Notifications;
using qqbot.Services.Images;

namespace qqbot.Handlers;

/// <summary>
/// 图像下载处理器 - 自动下载并缓存消息中的图像
/// </summary>
public class ImageDownloadHandler : 
    INotificationHandler<GroupMessageReceivedNotification>,
    INotificationHandler<PrivateMessageReceivedNotification>
{
    private readonly ILogger<ImageDownloadHandler> _logger;
    private readonly ImageDownloadCache _imageDownloadCache;
    private readonly ImageCacheHelper _imageCacheHelper;
    
    public ImageDownloadHandler(
        ILogger<ImageDownloadHandler> logger, 
        ImageDownloadCache imageDownloadCache,
        ImageCacheHelper imageCacheHelper)
    {
        _imageDownloadCache = imageDownloadCache;
        _imageCacheHelper = imageCacheHelper;
        _logger = logger;
    }

    /// <summary>
    /// 处理群消息中的图像
    /// </summary>
    public async Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var messageEvent = notification.MessageEvent;
            var messageSegments = messageEvent.Message;
            
            _logger.LogInformation("处理群消息图像下载，群ID: {GroupId}, 用户ID: {UserId}", 
                messageEvent.GroupId, messageEvent.UserId);

            await ProcessImageSegments(messageSegments, "group", messageEvent.GroupId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理群消息图像下载时发生错误");
        }
    }

    /// <summary>
    /// 处理私聊消息中的图像
    /// </summary>
    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var messageEvent = notification.MessageEvent;
            var messageSegments = messageEvent.Message;
            
            _logger.LogInformation("处理私聊消息图像下载，用户ID: {UserId}", messageEvent.UserId);

            await ProcessImageSegments(messageSegments, "private", messageEvent.UserId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理私聊消息图像下载时发生错误");
        }
    }

    /// <summary>
    /// 处理消息段中的图像
    /// </summary>
    private async Task ProcessImageSegments(List<qqbot.Models.Messages.MessageSegment> messageSegments, string messageType, string contextId)
    {
        var imageSegments = _imageCacheHelper.ExtractImageSegments(messageSegments);

        if (!imageSegments.Any())
        {
            _logger.LogDebug("消息中没有找到图像段");
            return;
        }

        _logger.LogInformation("在{MessageType}消息中发现 {Count} 个图像段", messageType, imageSegments.Count);

        foreach (var imageSegment in imageSegments)
        {
            try
            {
                var imageData = imageSegment.Data!;
                var imageUrl = _imageCacheHelper.GetImageUrl(imageData);
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogWarning("图像段中没有有效的URL");
                    continue;
                }

                // 验证URL有效性
                if (!_imageCacheHelper.IsValidImageUrl(imageUrl))
                {
                    _logger.LogWarning("图像URL无效: {ImageUrl}", imageUrl);
                    continue;
                }

                // 生成缓存文件名
                var fileName = _imageCacheHelper.GenerateCacheFileName(imageUrl, messageType, contextId);
                
                _logger.LogInformation("开始下载图像: {ImageUrl} -> {FileName}", imageUrl, fileName);
                
                // 下载并缓存图像
                await _imageDownloadCache.DownloadImage(imageUrl, fileName);
                
                _logger.LogInformation("图像下载完成: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载图像时发生错误: {ImageFile}", imageSegment.Data?.File);
            }
        }
    }

}
