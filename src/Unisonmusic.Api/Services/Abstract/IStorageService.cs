namespace Unisonmusic.Api.Services.Abstract
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(FileInfo fileInfo, string objectKey = null, CancellationToken cancellationToken = default);
        string GetPresignedUrl(string objectKey, double expiresHours = 1);
    }
}
