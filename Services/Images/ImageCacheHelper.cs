using qqbot.Models.Messages;

namespace qqbot.Services.Images;

/// <summary>
/// 图像缓存辅助服务 - 提供图像下载和缓存相关的工具方法
/// </summary>
public class ImageCacheHelper
{
    private readonly ILogger<ImageCacheHelper> _logger;

    public ImageCacheHelper(ILogger<ImageCacheHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 从消息段中提取图像段
    /// </summary>
    public List<ImageMessageSegment> ExtractImageSegments(List<MessageSegment> messageSegments)
    {
        return messageSegments
            .OfType<ImageMessageSegment>()
            .Where(img => img.Data != null && !string.IsNullOrEmpty(img.Data.File))
            .ToList();
    }

    /// <summary>
    /// 获取图像URL
    /// </summary>
    public string GetImageUrl(ImageData imageData)
    {
        if (!string.IsNullOrEmpty(imageData.Url))
        {
            return imageData.Url;
        }
        
        if (!string.IsNullOrEmpty(imageData.File))
        {
            if (imageData.File.StartsWith("http://") || imageData.File.StartsWith("https://"))
            {
                return imageData.File;
            }
        }
        
        return string.Empty;
    }

    /// <summary>
    /// 验证图像URL是否有效
    /// </summary>
    public bool IsValidImageUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// 生成缓存文件名
    /// </summary>
    public string GenerateCacheFileName(string imageUrl, string messageType, string contextId)
    {
        try
        {
            var uri = new Uri(imageUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
            {
                fileName = "image.jpg";
            }
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var hash = imageUrl.GetHashCode().ToString("X");
            var extension = Path.GetExtension(fileName);
            
            return $"{messageType}_{contextId}_{timestamp}_{hash}{extension}";
        }
        catch
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"image_{messageType}_{contextId}_{timestamp}.jpg";
        }
    }

    /// <summary>
    /// 根据图片名称搜索本地图片文件
    /// </summary>
    public List<ImageFileInfo> SearchLocalImages(string imageName, string? searchDirectory = null, bool exactMatch = false)
    {
        var results = new List<ImageFileInfo>();
        var cacheDir = searchDirectory ?? Path.Combine(AppContext.BaseDirectory, "ImageCache");
        
        if (!Directory.Exists(cacheDir))
        {
            return results;
        }

        var searchPattern = exactMatch ? imageName : $"*{imageName}*";
        var files = Directory.GetFiles(cacheDir, searchPattern, SearchOption.AllDirectories)
            .Where(IsImageFile)
            .ToList();

        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                results.Add(new ImageFileInfo
                {
                    FileName = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    FileSize = fileInfo.Length,
                    CreationTime = fileInfo.CreationTime,
                    LastModifiedTime = fileInfo.LastWriteTime,
                    ImageType = GetImageTypeFromFile(file),
                    MimeType = GetMimeTypeFromExtension(fileInfo.Extension)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取图片文件信息失败: {FilePath}", file);
            }
        }

        return results;
    }

    /// <summary>
    /// 根据消息类型和上下文ID搜索图片
    /// </summary>
    public List<ImageFileInfo> SearchImagesByContext(string messageType, string contextId, string? searchDirectory = null)
    {
        var results = new List<ImageFileInfo>();
        var cacheDir = searchDirectory ?? Path.Combine(AppContext.BaseDirectory, "ImageCache");
        
        if (!Directory.Exists(cacheDir))
        {
            return results;
        }

        var searchPattern = $"{messageType}_{contextId}_*";
        var files = Directory.GetFiles(cacheDir, searchPattern, SearchOption.AllDirectories)
            .Where(IsImageFile)
            .ToList();

        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                results.Add(new ImageFileInfo
                {
                    FileName = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    FileSize = fileInfo.Length,
                    CreationTime = fileInfo.CreationTime,
                    LastModifiedTime = fileInfo.LastWriteTime,
                    ImageType = GetImageTypeFromFile(file),
                    MimeType = GetMimeTypeFromExtension(fileInfo.Extension)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取图片文件信息失败: {FilePath}", file);
            }
        }

        return results;
    }

    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg" };
        return imageExtensions.Contains(extension);
    }

    private ImageType GetImageTypeFromFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => ImageType.JPEG,
            ".png" => ImageType.PNG,
            ".gif" => ImageType.GIF,
            ".bmp" => ImageType.BMP,
            ".tiff" => ImageType.TIFF,
            ".webp" => ImageType.WEBP,
            ".svg" => ImageType.SVG,
            _ => ImageType.Unknown
        };
    }

    private string GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" => "image/tiff",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// 图片文件信息
/// </summary>
public class ImageFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime LastModifiedTime { get; set; }
    public ImageType ImageType { get; set; }
    public string MimeType { get; set; } = string.Empty;

    public string GetFormattedFileSize()
    {
        if (FileSize < 1024)
            return $"{FileSize} B";
        else if (FileSize < 1024 * 1024)
            return $"{FileSize / 1024.0:F1} KB";
        else if (FileSize < 1024 * 1024 * 1024)
            return $"{FileSize / (1024.0 * 1024.0):F1} MB";
        else
            return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }

    public string GetRelativePath(string cacheDirectory)
    {
        if (FullPath.StartsWith(cacheDirectory))
        {
            return FullPath.Substring(cacheDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
        }
        return FileName;
    }
}

/// <summary>
/// 图片类型枚举
/// </summary>
public enum ImageType
{
    Unknown = 0,
    JPEG = 1,
    PNG = 2,
    GIF = 3,
    BMP = 4,
    TIFF = 5,
    WEBP = 6,
    SVG = 7
}
