namespace Unisonmusic.Api.Services.Abstract
{
    public interface IOfftubeClient
    {
        Task<string> GetFileKeyAsync(string url, CancellationToken cancellationToken = default);
    }
}
