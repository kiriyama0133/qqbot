using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace qqbot.Services // 请确保命名空间正确
{
    public record DownloadProgress(long BytesDownloaded, long TotalBytes);

    public class FileCacheHttpService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FileCacheHttpService> _logger;
        public int Retries { get; set; } = 5;

        public FileCacheHttpService(HttpClient httpClient, ILogger<FileCacheHttpService> logger)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromHours(1); // 保持一个较长的超时
        }

        public async Task DownlaodFileAsync(string url, string destinationPath,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default
            )
        {
            for (int i = 0; i < Retries; i++)
            {
                try
                {
                    _logger.LogInformation("开始缓存文件 {Url}", url);
                    await PerformDownloadAsync(url, destinationPath, progress, cancellationToken);
                    _logger.LogInformation("文件 {DestinationPath} 下载成功。", destinationPath);

                    // 成功后立即返回，不再继续循环。
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "第 {Attempt} 次下载失败。将在5秒后重试...", i + 1);
                    if (i == Retries - 1)
                    {
                        _logger.LogError(ex, "已达到最大重试次数，下载文件 {Url} 失败。", url);
                        throw; // 达到最大次数后，将异常抛出
                    }
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }

        private async Task PerformDownloadAsync(
            string url, string destinationPath, // 修正了参数名的拼写错误
            IProgress<DownloadProgress>? progress,
            CancellationToken cancellationToken
            )
        {
            long existingFileSize = 0;
            if (File.Exists(destinationPath))
            {
                existingFileSize = new FileInfo(destinationPath).Length;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (existingFileSize > 0)
            {
                request.Headers.Range = new RangeHeaderValue(existingFileSize, null);
                _logger.LogInformation("文件已存在，从 {Bytes} 字节处继续下载。", existingFileSize);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                existingFileSize = 0;
            }
            else if (response.StatusCode != System.Net.HttpStatusCode.PartialContent && existingFileSize > 0)
            {
                throw new InvalidOperationException($"服务器不支持断点续传 (状态码: {response.StatusCode})");
            }
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            if (existingFileSize > 0 && response.Content.Headers.ContentRange != null)
            {
                totalBytes = response.Content.Headers.ContentRange.Length ?? 0;
            }
            _logger.LogInformation("开始下载，总大小 {TotalBytes} 字节。", totalBytes);

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(destinationPath, existingFileSize == 0 ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long totalBytesRead = existingFileSize;
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;
                progress?.Report(new DownloadProgress(totalBytesRead, totalBytes));
            }
        }

        public void Dispose()
        {
            // 正确实现 Dispose
            _httpClient?.Dispose();
        }
    }
}