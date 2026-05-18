namespace Unisonmusic.Api.Models.Dtos
{
    public class DownloadedTrackDto
    {
        public long Id { get; set; }

        public string Url { get; set; } = string.Empty;

        public string S3Url { get; set; } = string.Empty;

        public string TrackTitle { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
