namespace Unisonmusic.Api.Services.Abstract
{
    public interface ITrackDownloadLockService
    {
        Task<IDisposable> AcquireAsync(string url, CancellationToken cancellationToken);
    }
}
