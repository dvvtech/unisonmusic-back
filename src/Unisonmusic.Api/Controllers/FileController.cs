using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unisonmusic.Api.DAL;
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

        public FileController(
            IOfftubeClient offtubeClient,
            IStorageService storageService,
            UnisonmusicDbContext dbContext)
        {
            _offtubeClient = offtubeClient;
            _storageService = storageService;
            _dbContext = dbContext;
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

            request.Url = request.Url.Trim();

            var existingTrack = await _dbContext.Tracks
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Url == request.Url,
                    cancellationToken);

            if (existingTrack != null)
            {
                var s3ObjectUrl = _storageService.GetPresignedUrl(existingTrack.S3ObjectKey);

                return Ok(new UrlS3Response
                {
                    Url = s3ObjectUrl
                });
            }
            
            var objectKey = await _offtubeClient.GetFileKeyAsync(
                request.Url,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(objectKey))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to upload file");
            }

            try
            {
                await _dbContext.Tracks.AddAsync(new DAL.Entities.TrackEntity
                {
                    Url = request.Url,
                    S3ObjectKey = objectKey,
                });

                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // запись уже вставлена другим запросом

                var savedTrack = await _dbContext.Tracks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.Url == request.Url,
                        cancellationToken);

                if (savedTrack == null)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "Failed to get saved track");
                }

                objectKey = savedTrack.S3ObjectKey;
            }

            var s3Url = _storageService.GetPresignedUrl(objectKey);

            return Ok(new UrlS3Response
            {
                Url = s3Url
            });
        }
    }
}
