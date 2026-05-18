using System.ComponentModel.DataAnnotations;

namespace Unisonmusic.Api.Configuration
{
    public sealed class JwtConfig
    {
        public const string SectionName = "Jwt";

        [Required, MinLength(10)]
        public string Key { get; init; }

        [Required]
        public string Issuer { get; init; }

        [Required]
        public string Audience { get; init; }

        [Required]
        public TimeSpan ExpiresDuration { get; init; }
    }
}
