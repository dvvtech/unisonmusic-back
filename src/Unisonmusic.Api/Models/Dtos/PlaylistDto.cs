namespace Unisonmusic.Api.Models.Dtos
{
    public class PlaylistDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int TrackCount { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
