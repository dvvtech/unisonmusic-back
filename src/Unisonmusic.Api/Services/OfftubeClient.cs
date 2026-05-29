using System.Text;
using System.Text.Json;
using Unisonmusic.Api.Models;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Services
{
    public class OfftubeClient : IOfftubeClient
    {
        private readonly HttpClient _httpClient;        
        private readonly ILogger<OfftubeClient> _logger;

        public OfftubeClient(
            HttpClient httpClient,            
            ILogger<OfftubeClient> logger)
        {
            _httpClient = httpClient;     
            _logger = logger;
        }

        public async Task<UploadResponse> GetFileKeyAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    Url = url
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "music/upload-from-url",
                    request,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var result = await response.Content
                    .ReadFromJsonAsync<UploadResponse>(cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file from url: {Url}", url);
            }

            return null;
        }

        public async Task<UploadResponse> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var fileStream = file.OpenReadStream();
                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(fileStream);

                if (!string.IsNullOrWhiteSpace(file.ContentType))
                {
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                }

                form.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync(
                    "music/upload-from-file",
                    form,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                return await response.Content
                    .ReadFromJsonAsync<UploadResponse>(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {FileName}", file.FileName);
            }

            return null;
        }
    }
}
