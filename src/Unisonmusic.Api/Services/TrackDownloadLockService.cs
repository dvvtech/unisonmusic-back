using System.Collections.Concurrent;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Services
{
    public class TrackDownloadLockService : ITrackDownloadLockService
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async Task<IDisposable> AcquireAsync(
            string url,
            CancellationToken cancellationToken)
        {
            var semaphore = _locks.GetOrAdd(
                url,
                _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken);

            return new Releaser(url, semaphore, _locks);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly string _url;
            private readonly SemaphoreSlim _semaphore;

            private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

            private bool _disposed;

            public Releaser(
                string url,
                SemaphoreSlim semaphore,
                ConcurrentDictionary<string, SemaphoreSlim> locks)
            {
                _url = url;
                _semaphore = semaphore;
                _locks = locks;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                _semaphore.Release();

                if (_semaphore.CurrentCount == 1)
                {
                    _locks.TryRemove(_url, out _);
                    _semaphore.Dispose();
                }
            }
        }
    }
}
