using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unisonmusic.Api.DAL;
using Unisonmusic.Api.DAL.Entities;
using Unisonmusic.Api.Extensions;
using Unisonmusic.Api.Models;
using Unisonmusic.Api.Models.Dtos;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Controllers
{
    [Route("playlists")]
    [ApiController]
    [Authorize]
    public class PlaylistsController : ControllerBase
    {
        public const string LikedPlaylistName = "Понравившиеся";

        private readonly UnisonmusicDbContext _dbContext;
        private readonly IStorageService _storageService;

        public PlaylistsController(
            UnisonmusicDbContext dbContext,
            IStorageService storageService)
        {
            _dbContext = dbContext;
            _storageService = storageService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<PlaylistDto>>> GetPlaylists(
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentAccountId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            await EnsureDefaultPlaylistExistsAsync(userId.Value, cancellationToken);

            var playlists = await _dbContext.Playlists
                .AsNoTracking()
                .Where(x => x.UserId == userId.Value)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new PlaylistDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    CreatedAtUtc = x.CreatedAtUtc,
                    TrackCount = x.PlaylistTracks.Count
                })
                .ToListAsync(cancellationToken);

            return Ok(playlists);
        }

        [HttpPost]
        public async Task<ActionResult<PlaylistDto>> CreatePlaylist(
            [FromBody] CreatePlaylistRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentAccountId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var name = request?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Playlist name is required");
            }

            if (name.Length > 128)
            {
                return BadRequest("Playlist name is too long");
            }

            await EnsureDefaultPlaylistExistsAsync(userId.Value, cancellationToken);

            var alreadyExists = await _dbContext.Playlists
                .AnyAsync(
                    x => x.UserId == userId.Value &&
                         x.Name == name,
                    cancellationToken);

            if (alreadyExists)
            {
                return Conflict("Playlist already exists");
            }

            var playlist = new PlaylistEntity
            {
                UserId = userId.Value,
                Name = name,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dbContext.Playlists.AddAsync(playlist, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new PlaylistDto
            {
                Id = playlist.Id,
                Name = playlist.Name,
                CreatedAtUtc = playlist.CreatedAtUtc,
                TrackCount = 0
            });
        }

        [HttpGet("{playlistId:long}/tracks")]
        public async Task<ActionResult<IReadOnlyCollection<DownloadedTrackDto>>> GetPlaylistTracks(
            long playlistId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentAccountId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var playlistExists = await _dbContext.Playlists
                .AnyAsync(
                    x => x.Id == playlistId &&
                         x.UserId == userId.Value,
                    cancellationToken);

            if (!playlistExists)
            {
                return NotFound("Playlist not found");
            }

            var tracks = await _dbContext.PlaylistTracks
                .AsNoTracking()
                .Where(x => x.PlaylistId == playlistId)
                .OrderByDescending(x => x.AddedAtUtc)
                .Select(x => new
                {
                    x.TrackId,
                    x.AddedAtUtc,
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
                CreatedAtUtc = x.AddedAtUtc,
                S3Url = _storageService.GetPresignedUrl(x.S3ObjectKey)
            }));
        }

        [HttpPost("{playlistId:long}/tracks")]
        public async Task<ActionResult> AddTrackToPlaylist(
            long playlistId,
            [FromBody] AddTrackToPlaylistRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentAccountId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            if (request == null || request.TrackId <= 0)
            {
                return BadRequest("TrackId is required");
            }

            var playlistExists = await _dbContext.Playlists
                .AnyAsync(
                    x => x.Id == playlistId &&
                         x.UserId == userId.Value,
                    cancellationToken);

            if (!playlistExists)
            {
                return NotFound("Playlist not found");
            }

            var userTrackExists = await _dbContext.UserTracks
                .AnyAsync(
                    x => x.UserId == userId.Value &&
                         x.TrackId == request.TrackId,
                    cancellationToken);

            if (!userTrackExists)
            {
                return BadRequest("Track is not downloaded by current user");
            }

            var playlistTrackExists = await _dbContext.PlaylistTracks
                .AnyAsync(
                    x => x.PlaylistId == playlistId &&
                         x.TrackId == request.TrackId,
                    cancellationToken);

            if (playlistTrackExists)
            {
                return Ok();
            }

            await _dbContext.PlaylistTracks.AddAsync(
                new PlaylistTrackEntity
                {
                    PlaylistId = playlistId,
                    TrackId = request.TrackId,
                    AddedAtUtc = DateTime.UtcNow
                },
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpDelete("{playlistId:long}")]
        public async Task<ActionResult> DeletePlaylist(
            long playlistId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentAccountId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var playlist = await _dbContext.Playlists
                .FirstOrDefaultAsync(
                    x => x.Id == playlistId &&
                         x.UserId == userId.Value,
                    cancellationToken);

            if (playlist == null)
            {
                return NotFound("Playlist not found");
            }

            _dbContext.Playlists.Remove(playlist);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        private async Task EnsureDefaultPlaylistExistsAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var exists = await _dbContext.Playlists
                .AnyAsync(
                    x => x.UserId == userId &&
                         x.Name == LikedPlaylistName,
                    cancellationToken);

            if (exists)
            {
                return;
            }

            try
            {
                await _dbContext.Playlists.AddAsync(
                    new PlaylistEntity
                    {
                        UserId = userId,
                        Name = LikedPlaylistName,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another request created the default playlist first.
            }
        }
    }
}
