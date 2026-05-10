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

        public async Task<string> GetFileKeyAsync(string url, CancellationToken cancellationToken = default)
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

                return result.ObjectKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file from url: {Url}", url);
            }

            return string.Empty;
        }
    }
}
