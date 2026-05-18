using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unisonmusic.Api.DAL;
using Unisonmusic.Api.Extensions;
using Unisonmusic.Api.Models.Dtos;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Controllers
{
    [Route("tracks")]
    [ApiController]
    [Authorize]
    public class TracksController : ControllerBase
    {
        private readonly UnisonmusicDbContext _dbContext;
        private readonly IStorageService _storageService;

        public TracksController(
            UnisonmusicDbContext dbContext,
            IStorageService storageService)
        {
            _dbContext = dbContext;
            _storageService = storageService;
        }

        [HttpGet("downloaded")]
        public async Task<ActionResult<IReadOnlyCollection<DownloadedTrackDto>>> GetDownloadedTracks(
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentAccountId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var tracks = await _dbContext.UserTracks
                .AsNoTracking()
                .Where(x => x.UserId == userId.Value)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.TrackId,
                    x.CreatedAtUtc,
                    x.Track.Url,
                    x.Track.Title,
                    x.Track.S3ObjectKey
                })
                .ToListAsync(cancellationToken);

            return Ok(tracks.Select(x => new DownloadedTrackDto
            {
                Id = x.TrackId,
                Url = x.Url,
                TrackTitle = x.Title,
                CreatedAtUtc = x.CreatedAtUtc,
                S3Url = _storageService.GetPresignedUrl(x.S3ObjectKey)
            }));
        }
    }
}
