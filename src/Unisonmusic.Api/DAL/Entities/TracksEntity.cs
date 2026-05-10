namespace Unisonmusic.Api.DAL.Entities
{
    public class TracksEntity
    {
        public long Id { get; set; }

        public string Url { get; set; }

        public string S3ObjectKey { get; set; }
    }
}
