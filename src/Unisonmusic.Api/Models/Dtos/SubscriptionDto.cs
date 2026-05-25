namespace Unisonmusic.Api.Models.Dtos;

public class SubscriptionDto
{
    public string Plan { get; set; } = "basic";

    public string DisplayName { get; set; } = "Базовый";

    public bool IsActive { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public SubscriptionLimitsDto Limits { get; set; } = new();
}

public class SubscriptionLimitsDto
{
    public int MaxPlaylists { get; set; }

    public int MaxRoomMembers { get; set; }

    public bool BackgroundPlay { get; set; }
}
