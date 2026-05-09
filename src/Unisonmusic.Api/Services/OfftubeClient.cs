namespace Unisonmusic.Api.Services
{
    public class OfftubeClient : IOfftubeClient
    {
        private const string url = "http://offtube_api:8080/music/upload-from-url";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OfftubeClient> _logger;

        public OfftubeClient(
            IHttpClientFactory httpClientFactory,
            ILogger<OfftubeClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task GetFileKeyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                using var request = new HttpRequestMessage(HttpMethod.Get, url);                

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Analytics tracking failed with status code {StatusCode}",
                        response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track visit");
            }
        }
    }
}
