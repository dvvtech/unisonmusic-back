namespace Unisonmusic.Api.DAL.Entities
{
    public class PlaylistEntity
    {
        public long Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public ICollection<PlaylistTrackEntity> PlaylistTracks { get; set; }
            = new List<PlaylistTrackEntity>();
    }
}
