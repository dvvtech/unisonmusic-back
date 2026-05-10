namespace Unisonmusic.Api.DAL.Entities
{
    public class TrackEntity
    {
        public long Id { get; set; }
        
        public string Url { get; set; }

        public string S3ObjectKey { get; set; }

        public string Title { get; set; }
    }
}
