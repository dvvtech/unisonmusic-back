namespace Unisonmusic.Api.Services
{
    public interface IOfftubeClient
    {
        Task GetFileKeyAsync(CancellationToken cancellationToken = default);
    }
}
