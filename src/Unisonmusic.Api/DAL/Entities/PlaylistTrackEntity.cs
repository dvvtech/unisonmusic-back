namespace Unisonmusic.Api.DAL.Entities
{
    public class PlaylistTrackEntity
    {
        public long PlaylistId { get; set; }

        public long TrackId { get; set; }

        public DateTime AddedAtUtc { get; set; }

        public PlaylistEntity Playlist { get; set; } = null!;

        public TrackEntity Track { get; set; } = null!;
    }
}
