using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unisonmusic.Api.DAL;
using Unisonmusic.Api.DAL.Entities;
using Unisonmusic.Api.Models;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Controllers
{
    [Route("file")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IOfftubeClient _offtubeClient;
        private readonly IStorageService _storageService;
        private readonly UnisonmusicDbContext _dbContext;
        private readonly ITrackDownloadLockService _lockService;

        public FileController(
            IOfftubeClient offtubeClient,
            IStorageService storageService,
            UnisonmusicDbContext dbContext,
            ITrackDownloadLockService lockService)
        {
            _offtubeClient = offtubeClient;
            _storageService = storageService;
            _dbContext = dbContext;
            _lockService = lockService;
        }

        [HttpPost("upload-from-url")]
        public async Task<IActionResult> UploadFromUrl(
            [FromBody] UrlRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Url))
            {
                return BadRequest("Url is required");
            }

            // TODO:
            // получать из JWT/Auth
            int userId = 1;

            request.Url = request.Url.Trim();

            // Первая проверка БД без lock
            var existingTrack = await _dbContext.Tracks
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Url == request.Url,
                    cancellationToken);

            if (existingTrack != null)
            {
                await EnsureUserTrackExistsAsync(
                    userId,
                    existingTrack.Id,
                    cancellationToken);

                return Ok(CreateResponse(existingTrack));
            }

            // lock по url
            using var downloadLock = await _lockService.AcquireAsync(
                request.Url,
                cancellationToken);

            // ВАЖНО:
            // повторная проверка после ожидания lock
            existingTrack = await _dbContext.Tracks
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Url == request.Url,
                    cancellationToken);

            if (existingTrack != null)
            {
                await EnsureUserTrackExistsAsync(
                    userId,
                    existingTrack.Id,
                    cancellationToken);

                return Ok(CreateResponse(existingTrack));
            }

            // Реально качаем только здесь
            var uploadResponse = await _offtubeClient.GetFileKeyAsync(
                request.Url,
                cancellationToken);

            if (uploadResponse == null ||
                string.IsNullOrWhiteSpace(uploadResponse.ObjectKey))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to upload file");
            }

            TrackEntity trackEntity;

            try
            {
                trackEntity = new TrackEntity
                {
                    Url = request.Url,
                    S3ObjectKey = uploadResponse.ObjectKey,
                    Title = uploadResponse.TrackTitle
                };

                await _dbContext.Tracks.AddAsync(
                    trackEntity,
                    cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // На случай race condition между инстансами (другой запрос уже создал Track)                
                trackEntity = await _dbContext.Tracks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.Url == request.Url,
                        cancellationToken);

                if (trackEntity == null)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "Failed to get saved track");
                }
            }

            await EnsureUserTrackExistsAsync(
                userId,
                trackEntity.Id,
                cancellationToken);

            return Ok(CreateResponse(trackEntity));
        }

        private async Task EnsureUserTrackExistsAsync(
            int userId,
            long trackId,
            CancellationToken cancellationToken)
        {
            var exists = await _dbContext.UserTracks
                .AnyAsync(
                    x => x.UserId == userId &&
                         x.TrackId == trackId,
                    cancellationToken);

            if (exists)
            {
                return;
            }

            try
            {
                await _dbContext.UserTracks.AddAsync(
                    new UserTrackEntity
                    {
                        UserId = userId,
                        TrackId = trackId,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // другой запрос уже создал связь
            }
        }

        private UrlS3Response CreateResponse(TrackEntity track)
        {
            return new UrlS3Response
            {
                Url = track.Url,
                S3Url = _storageService.GetPresignedUrl(
                    track.S3ObjectKey),
                TrackTitle = track.Title
            };
        }
    }
}
