using MediatR;
using System.Net.Http.Headers;
using System.Reactive.Disposables;

namespace qqbot.Services;

public record DownloadProgress(long BytesDownload, long TotalBytes);
public class FileCacheHttpService : IDisposable
{
    private readonly ILogger<FileCacheHttpService> _logger;
    private readonly HttpClient _httpClient;
    public int Retries { get; set; } = 5;

    public FileCacheHttpService(HttpClient httpClient, ILogger<FileCacheHttpService> logger){
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(5); // 超时时间
    }

    // 异步下载逻辑
    public async Task DownlaodFileAsync(string url, string destinationPath, 
        IProgress<DownloadProgress>? progress = null, 
        CancellationToken cancellationToken = default
        )
    {
        for (int i = 0; i < Retries; i++){
            try
            {
                _logger.LogInformation("开始缓存文件{url}", url);
                await PerformDownloadAsync(url, destinationPath, progress, cancellationToken);
                _logger.LogInformation("文件{DestinationPath}下载成功", destinationPath);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "第{attempt}次重试...", i + 1);
                if ( i== Retries - 1){
                    _logger.LogError(ex, "达到最大的重试次数，下载文件{}失败", url);
                }
                await Task.Delay(5000, cancellationToken); // 5s后重试
            }
        }
    }

    private async Task PerformDownloadAsync(
        string url, string desinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken
        )
    {
        long existingFileSize = 0;
        // 检查下载点
        if (File.Exists(desinationPath)){
            existingFileSize = new FileInfo(desinationPath).Length;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url); // 创建Url的http请求
        if (existingFileSize > 0){
            request.Headers.Range = new RangeHeaderValue(existingFileSize, null);
            _logger.LogInformation("文件已经存在，开始从{Bytes字节下载}", existingFileSize);
        }

        // 确保先获取响应头，而不会缓冲整个响应体
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.OK){ 

            existingFileSize = 0; //
        }
        else if (response.StatusCode != System.Net.HttpStatusCode.PartialContent && existingFileSize > 0) {
            throw new InvalidOperationException($"服务器不支持断点续传 (状态码: {response.StatusCode})");
        }
        response.EnsureSuccessStatusCode();

        long totalBytes = response.Content.Headers.ContentLength ?? 0;
        if ( existingFileSize >0 && response.Content.Headers.ContentRange != null){
            totalBytes = response.Content.Headers.ContentRange.Length ?? 0;
        }
        _logger.LogInformation("开始下载，总大小{TotalBytes}字节", totalBytes);

        // 异步读写数据流
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        // append模式追踪数据
        using var fileStream = new FileStream(desinationPath,existingFileSize == 0 ? FileMode.Create : FileMode.Append , FileAccess.Write, FileShare.None);
        var buffer = new byte[81920]; // 80KB 缓冲区
        long totalBytesRead = existingFileSize;
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0) {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken); // 
            totalBytesRead += bytesRead;
            progress?.Report(new DownloadProgress(totalBytesRead, totalBytes));
        }
    }
    

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

// 使用例程
//public class SomeFileProcessingHandler : INotificationHandler<...>
//{
//    private readonly FileDownloaderService _downloader;

//    public SomeFileProcessingHandler(FileDownloaderService downloader)
//    {
//        _downloader = downloader;
//    }

//    public async Task Handle(...)
//    {
//        string url = "http://example.com/large-file.zip";
//        string savePath = @"C:\Downloads\large-file.zip";

//        // 创建一个 Progress 对象来接收进度更新
//        var progress = new Progress<DownloadProgress>(p =>
//        {
//            // 这个回调会在 UI 线程或调用者线程上执行
//            double percentage = (double)p.BytesDownloaded / p.TotalBytes * 100;
//            Console.WriteLine($"下载进度: {percentage:F2}% ({p.BytesDownloaded} / {p.TotalBytes})");
//        });

//        try
//        {
//            await _downloader.DownloadFileAsync(url, savePath, progress);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"下载最终失败: {ex.Message}");
//        }
//    }
//}