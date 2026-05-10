using Unisonmusic.Api.Models;

namespace Unisonmusic.Api.Services.Abstract
{
    public interface IOfftubeClient
    {
        Task<UploadResponse> GetFileKeyAsync(string url, CancellationToken cancellationToken = default);
    }
}
