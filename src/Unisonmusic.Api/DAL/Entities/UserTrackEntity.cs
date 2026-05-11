namespace Unisonmusic.Api.DAL.Entities
{
    public class UserTrackEntity
    {
        /// <summary>
        /// Id пользователя из внешней системы/БД
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Id трека
        /// </summary>
        public long TrackId { get; set; }

        /// <summary>
        /// Когда пользователь добавил трек
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Навигационное свойство
        /// </summary>
        public TrackEntity Track { get; set; }
    }
}
