using qqbot.Core.Services;
using System.Threading.Tasks;

namespace qqbot.Services.Images;

public class ImageDownloadCache
{
    private readonly FileCacheHttpService _fileCacheHttpService;
    private readonly IDynamicStateService _dynamicStateService;
    private readonly string DownloadDicPath = Path.Combine(AppContext.BaseDirectory, "ImageCache");
    private static readonly string[] ValidImageMimeTypes = new[]
{
    "image/jpeg",
    "image/png",
    "image/gif",
    "image/bmp",
    "image/tiff",
    "image/webp",
    // ⚠️ 注意：如果你需要处理 SVG 或 ICO，它们也有特定的 MIME 类型
    // "image/svg+xml",
    // "image/x-icon" 
};
    public int DownloadedNumber {  get; set; } = 0;
    public ImageDownloadCache(FileCacheHttpService fileCacheHttpService, IDynamicStateService dynamicStateService)
    {
        _dynamicStateService = dynamicStateService;
        _fileCacheHttpService = fileCacheHttpService;
    }

    public async Task DownloadImage(string Url, string Path)
    {
        string download_file_path = PathBuilder(Path);
        if (!File.Exists(DownloadDicPath))
        {
            Directory.CreateDirectory(DownloadDicPath);
        }
        try
        {
            if (IsImage(Url).Result == false)
            {
                return;
            }
            // download image
            await _fileCacheHttpService.DownlaodFileAsync(Url, download_file_path, null, CancellationToken.None);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            DownloadedNumber += 1;
        }
    }

    public string PathBuilder(string Url)
    {
        string download_file_path = Path.Combine(AppContext.BaseDirectory, "ImageCache",Url);
        return download_file_path;
    }

    // examine the file type whether the image, using the Extension
    public async Task<bool> IsImage(string Url)
    {
        try
        {
            // 首先尝试通过 HTTP HEAD 请求获取内容类型
            string? mediaType = await _fileCacheHttpService.GetFileContentTypeAsync(Url);
            if (!string.IsNullOrEmpty(mediaType))
            {
                return ValidImageMimeTypes.Any(mime =>
                     string.Equals(mime, mediaType, StringComparison.OrdinalIgnoreCase)
                 );
            }

            // 如果无法获取内容类型，尝试通过 URL 扩展名判断
            var uri = new Uri(Url);
            var extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg" };
            
            if (imageExtensions.Contains(extension))
            {
                Console.WriteLine($"通过文件扩展名判断为图片: {extension}");
                return true;
            }

            // 如果 URL 包含图片相关的关键词，也认为是图片
            var urlLower = Url.ToLowerInvariant();
            if (urlLower.Contains("image") || urlLower.Contains("photo") || urlLower.Contains("pic") || 
                urlLower.Contains("multimedia") || urlLower.Contains("download"))
            {
                Console.WriteLine($"通过 URL 关键词判断为图片: {Url}");
                return true;
            }

            // 对于 QQ 的图片链接，如果无法确定类型，默认认为是图片
            if (urlLower.Contains("qq.com") || urlLower.Contains("nt.qq.com"))
            {
                Console.WriteLine($"QQ 图片链接，默认认为是图片: {Url}");
                return true;
            }

            Console.WriteLine($"无法确定文件类型，跳过下载: {Url}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检查图片类型时发生错误: {ex.Message}");
            // 发生错误时，默认尝试下载
            return true;
        }
    }
}
